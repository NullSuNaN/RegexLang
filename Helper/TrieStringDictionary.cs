using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RegexLang.Helper;

public class TrieStringDictionary<T> : IDictionary<string, T>
  where T : class
{
  private readonly TrieNode<T> _root = new();
  private int _version;

  public T this[string key]
  {
    get
    {
      if (key == null) throw new ArgumentNullException(nameof(key));
      if (TryGetValue(key, out var value)) return value;
      throw new KeyNotFoundException($"The key '{key}' was not found in the Trie.");
    }
    set
    {
      if (key == null) throw new ArgumentNullException(nameof(key));
      _root.InsertOrUpdate(key, 0, value);
      _version++;
    }
  }

  public ICollection<string> Keys => [.. GetAllKeysAndValues().Select(kvp => kvp.Key)];
  public ICollection<T> Values => [.. GetAllKeysAndValues().Select(kvp => kvp.Value)];
  public int Count => GetAllKeysAndValues().Count;
  public bool IsReadOnly => false;

  public void Add(string key, T value)
  {
    if (key == null) throw new ArgumentNullException(nameof(key));
    if (ContainsKey(key)) throw new ArgumentException($"An item with the same key has already been added: '{key}'");

    _root.InsertOrUpdate(key, 0, value);
    _version++;
  }

  public bool ContainsKey(string key)
  {
    if (key == null) throw new ArgumentNullException(nameof(key));

    TrieNode<T>? node = _root.FindNode(key, 0);
    return node != null && node.IsEndOfWord;
  }

  public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
  {
    if (key == null) throw new ArgumentNullException(nameof(key));

    TrieNode<T>? node = _root.FindNode(key, 0);
    if (node != null && node.IsEndOfWord)
    {
      value = node.Value!;
      return true;
    }

    value = null;
    return false;
  }

  public bool Remove(string key)
  {
    if (key == null) throw new ArgumentNullException(nameof(key));

    bool removed = _root.RemoveElement(key, 0);
    if (removed) _version++;
    return removed;
  }

  public void Clear()
  {
    _root.Children.Clear();
    _root.Value = null;
    _root.IsEndOfWord = false;
    _version++;
  }

  private List<KeyValuePair<string, T>> GetAllKeysAndValues()
  {
    var results = new List<KeyValuePair<string, T>>();
    _root.TraverseAndCollect("", results);
    return results;
  }

  #region Standard Collection Explicit Boilerplate 
  public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);
  public bool Contains(KeyValuePair<string, T> item) => TryGetValue(item.Key, out var val) && EqualityComparer<T>.Default.Equals(val, item.Value);
  public bool Remove(KeyValuePair<string, T> item) => Contains(item) && Remove(item.Key);

  public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
  {
    if (array == null) throw new ArgumentNullException(nameof(array));
    if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

    var pairs = GetAllKeysAndValues();
    if (array.Length - arrayIndex < pairs.Count) throw new ArgumentException("Destination array is too small.");

    for (int i = 0; i < pairs.Count; i++)
    {
      array[arrayIndex + i] = pairs[i];
    }
  }

  public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
  {
    int startVersion = _version;
    foreach (var pair in GetAllKeysAndValues())
    {
      if (startVersion != _version) throw new InvalidOperationException("Collection was mutated during configuration traversal loops.");
      yield return pair;
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  #endregion

  public ICollection<KeyValuePair<string, T>> GetAllWithPrefix(string prefix)
  {
    TrieNode<T>? node = _root.FindNode(prefix, 0);
    List<KeyValuePair<string, T>> result = [];
    if(node == null) return result;
    node.TraverseAndCollect(prefix, result);
    return result;
  }
}
