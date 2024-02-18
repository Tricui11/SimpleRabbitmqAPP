using System.Collections.Concurrent;

public class ConcurrentHashSet<T> {
  private readonly ConcurrentDictionary<T, bool> _dictionary = new ConcurrentDictionary<T, bool>();

  public bool TryAdd(T item) {
    return _dictionary.TryAdd(item, true);
  }

  public bool Contains(T item) {
    return _dictionary.ContainsKey(item);
  }

  public bool TryRemove(T item) {
    return _dictionary.TryRemove(item, out _);
  }
}
