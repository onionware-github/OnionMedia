using OnionMedia.Core.Enums;

namespace OnionMedia.Core.Services;

public interface ITaskbarProgressService
{
    ProgressBarState CurrentState { get; }
    void UpdateProgress(Type senderType, float progress);
    void UpdateState(Type senderType, ProgressBarState state);
    
    void SetType(Type type);
}