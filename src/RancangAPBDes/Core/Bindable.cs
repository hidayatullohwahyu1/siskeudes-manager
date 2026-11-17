using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RancangAPBDes.Core;

/// <summary>Objek yang memberi tahu tampilan bila nilainya berubah (tanpa memicu penyimpanan).</summary>
public abstract class Bindable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

/// <summary>Data yang disimpan ke file. Setiap perubahan memicu simpan otomatis.</summary>
public abstract class Model : Bindable
{
    protected override bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!base.Set(ref field, value, name)) return false;
        ChangeTracker.Notify();
        return true;
    }
}

public class TrackedCollection<T> : ObservableCollection<T>
{
    public TrackedCollection() { }
    public TrackedCollection(IEnumerable<T> items) : base(items) { }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        ChangeTracker.Notify();
    }
}

public static class ChangeTracker
{
    static int _suspend;
    public static event Action? Changed;
    public static bool IsSuspended => _suspend > 0;

    public static IDisposable Suspend()
    {
        _suspend++;
        return new Resumer();
    }

    public static void Notify()
    {
        if (_suspend == 0) Changed?.Invoke();
    }

    sealed class Resumer : IDisposable
    {
        bool _done;
        public void Dispose()
        {
            if (_done) return;
            _done = true;
            _suspend--;
        }
    }
}
