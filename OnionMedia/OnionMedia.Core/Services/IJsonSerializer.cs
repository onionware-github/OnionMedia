namespace OnionMedia.Core.Services;

public interface IJsonSerializer
{
    string Serialize(object? value);
    T Deserialize<T>(string json);

    Task<string> SerializeAsync(object? value);
    Task<T> DeserializeAsync<T>(string json);
}