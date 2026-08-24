namespace RegexLang.Helper;
using System.Collections.Generic;

public class TrieNode<T>
  where T : class
{
    public Dictionary<char, TrieNode<T>> Children { get; } = [];
    public T? Value { get; set; }
    public bool IsEndOfWord { get; set; }

    // Mutates and traverses down branches recursively to insert or update strings
    public void InsertOrUpdate(string key, int depth, T value)
    {
        if (depth == key.Length)
        {
            IsEndOfWord = true;
            Value = value;
            return;
        }

        char ch = key[depth];
        if (!Children.TryGetValue(ch, out var nextNode))
        {
            nextNode = new TrieNode<T>();
            Children[ch] = nextNode;
        }

        nextNode.InsertOrUpdate(key, depth + 1, value);
    }

    // Navigates down character branches to find a targeted endpoint node
    public TrieNode<T>? FindNode(string key, int depth)
    {
        if (depth == key.Length) return this;

        char ch = key[depth];
        if (Children.TryGetValue(ch, out var nextNode))
        {
            return nextNode.FindNode(key, depth + 1);
        }

        return null;
    }

    // Safely strips string markers and prunes empty structural sub-branches from memory
    public bool RemoveElement(string key, int depth)
    {
        if (depth == key.Length)
        {
            if (!IsEndOfWord) return false;

            IsEndOfWord = false;
            Value = null;
            return true;
        }

        char ch = key[depth];
        if (!Children.TryGetValue(ch, out var nextNode)) return false;

        bool shouldDeleteChild = nextNode.RemoveElement(key, depth + 1);

        if (shouldDeleteChild && !nextNode.IsEndOfWord && nextNode.Children.Count == 0)
        {
            Children.Remove(ch);
        }

        return shouldDeleteChild;
    }

    // Traverses down all children to find every allocated key-value pair under this node
    public void TraverseAndCollect(string currentPrefix, List<KeyValuePair<string, T>> results)
    {
        if (IsEndOfWord)
        {
            results.Add(new KeyValuePair<string, T>(currentPrefix, Value!));
        }

        foreach (var child in Children)
        {
            child.Value.TraverseAndCollect(currentPrefix + child.Key, results);
        }
    }
}
