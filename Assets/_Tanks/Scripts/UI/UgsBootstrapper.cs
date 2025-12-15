using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UgsBootstrapper : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        await InitAsync();
    }

    public static async Task InitAsync()
    {
        if (IsReady) return;

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        IsReady = true;
        Debug.Log($"UGS Ready. PlayerId={AuthenticationService.Instance.PlayerId}");
    }
}
