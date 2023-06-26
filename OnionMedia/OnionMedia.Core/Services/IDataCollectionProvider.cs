namespace OnionMedia.Core.Services;

public interface IDataCollectionProvider<out T>
{
    T[] GetItems();
}