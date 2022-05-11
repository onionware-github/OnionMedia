using System;

namespace OnionMedia.Contracts.Services
{
    public interface IPageService
    {
        Type GetPageType(string key);
    }
}
