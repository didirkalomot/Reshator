using System.Collections;
using System.Runtime.CompilerServices;

Console.WriteLine("Hello, World!");

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class Action : Attribute
{
    public string Name { get; }
    public string? ConditionName { get; }
    public int CountExtraArgs { get; }

    public Action(string name, string? conditionName = null, int countExtraArgs = 0)
    {
        Name = name;
        ConditionName = conditionName;
        CountExtraArgs = countExtraArgs;
    }
}

public abstract record Node : IEnumerable<Node>
{
    public Parent? parent { get; internal set; }

    public virtual IEnumerator<Node> GetEnumerator() { yield return this; }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Replace(Node? node) => parent?.ReplaceChild(this, node);

    public virtual Node Simplify() => this;

    public abstract Node Copy();

    public bool IsEquivalentTo(Node? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        Node left = Simplify(), right = other.Simplify();
        if (ReferenceEquals(left, right)) return true;
        return left.Equals(right);
    }
}

//////////////////////////////////////// Значения ////////////////////////////////////////

public abstract record Value<T>(T value) : Node;

public sealed record Number : Value<float>
{
    public Number(float value) : base(value) {}

    public override string ToString()
    {
        return float.IsInteger(value) ? $"{(int)value}" : $"{value:F4}";
    }

    public static Number operator -(Number a) => new Number(-a.value);
    public static Number operator +(Number a, Number b) => new Number(a.value + b.value);
    public static Number operator -(Number a, Number b) => new Number(a.value - b.value);
    public static Number operator *(Number a, Number b) => new Number(a.value * b.value);
    public static Number operator /(Number a, Number b) => new Number(a.value / b.value);

    public static bool operator <(Number a, Number b) => a.value < b.value;
    public static bool operator >(Number a, Number b) => a.value > b.value;
    public static bool operator <=(Number a, Number b) => a.value <= b.value;
    public static bool operator >=(Number a, Number b) => a.value >= b.value;
    
    public override Number Copy() => new Number(value) { parent = null };
}

public sealed record Letter : Value<string>
{
    public Letter(string value) : base(value) { }

    public override string ToString() => value;

    public override Letter Copy() => new Letter(value) { parent = null };
}

//////////////////////////////////////// Родители ////////////////////////////////////////

public abstract record Parent : Node
{
    protected List<Node> _operands = new List<Node>();
    public virtual IReadOnlyList<Node> operands => _operands;

    public abstract void ReplaceChild(Node? oldNode, Node? newNode);
}

public record Root : Parent
{
    public Node root { get; private set; }

    public override IReadOnlyList<Node> operands => new[] { root };

    public Root(Node root)
    {
        this.root = root;
        root.parent = this;
    }

    public override void ReplaceChild(Node? oldNode, Node? newNode)
    {
        if (newNode is null || !ReferenceEquals(root, oldNode) || oldNode == newNode) return;
        root.parent = null;
        root = newNode;
        newNode.parent = this;
    }

    public override IEnumerator<Node> GetEnumerator() => root.GetEnumerator();

    public override Node Copy() => root.Copy();
}

//////////////////////////////////////// Операторы ////////////////////////////////////////

public abstract record Operator : Parent
{
    public override IEnumerator<Node> GetEnumerator() { yield return this; } // переделать

    public Operator(IEnumerable<Node> operands)
    {
        _operands = operands.ToList();
        foreach (var op in _operands) op.parent = this;
    }

    public Operator(params Node[] operands) : this((IEnumerable<Node>)operands) { }

    public override void ReplaceChild(Node? oldNode, Node? newNode)
    {
        if (oldNode is null || newNode is null) return;
        int index = _operands.IndexOf(oldNode);
        if (index != -1) _operands[index] = newNode;
        newNode.parent = this;
        oldNode.parent = null;
    }

    public override Operator Copy()
    {
        var newOperands = _operands.Select(op => op.Copy()).ToArray();
        var copy = (Operator)Activator.CreateInstance(GetType(), newOperands)!;
        copy.parent = null;
        foreach (var op in copy._operands) op.parent = null;
        return copy;
    }
    
    public override Operator Simplify()
    {
        var copy = Copy();
        for (int i = 0; i < copy._operands.Count(); i++) 
            copy._operands[i] = _operands[i].Simplify();
        return copy;
    }

    public abstract Node? Result(); // переопределяется в конкретных операторах

    public void Work() => Replace(Result());
}

