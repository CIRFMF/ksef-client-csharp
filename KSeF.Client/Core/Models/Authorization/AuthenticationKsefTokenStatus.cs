
namespace KSeF.Client.Core.Models.Authorization;

public enum AuthenticationKsefTokenStatus
{
    /// <summary>
    /// Token został utworzony ale jest jeszcze w trakcie aktywacji i nadawania uprawnień. Nie może być jeszcze wykorzystywany do uwierzytelniania.
    /// </summary>
    Pending,

    /// <summary>
    /// Token jest aktywny i może być wykorzystywany do uwierzytelniania.
    /// </summary>
    Active,

    /// <summary>
    /// Token jest w trakcie unieważniania. Nie może już być wykorzystywany do uwierzytelniania.
    /// </summary>
    Revoking,

    /// <summary>
    /// Token został unieważniony i nie może być wykorzystywany do uwierzytelniania.
    /// </summary>
    Revoked,

    /// <summary>
    /// Nie udało się aktywować tokena. Należy wygenerować nowy token, obecny nie może być wykorzystywany do uwierzytelniania.
    /// </summary>
    Failed
}
