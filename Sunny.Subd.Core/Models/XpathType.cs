namespace Sunny.Subd.Core.Models
{
    public enum XpathType
    {
        Loading,
        Captcha,
        No_Internet,
        CP282,
        CP956,
        Logout,
        WrongPassword,
        Block,
        Success,
        TowFA,
        CashApp,
        InputUserName,
        InputPassword,
        NavigationButton,
        Regsiner_Facebook,
        ExistEmail,
        Confim_Register,

        // ── Pandora specific (verified on com.pandora.android v2504.1.1) ────
        PandoraWelcomeScreen,
        PandoraLoginButtons,
        PandoraFTUE,
        PandoraSearchElements,
        PandoraSearchInput,
        PandoraSoundTab,
        PandoraSongResult,
        PandoraPlayButton,
        PandoraLikeButton,
        PandoraBackButton,
        PandoraPopupDismiss,

        // Hộp thoại Meta "pay or consent" (EU) — xem XpathManagerFacebook +
        // FacebookHander.TryHandleMetaAdsConsentAsync. Để CUỐI enum để không dịch
        // giá trị các member đã có.
        MetaAdsConsent,
    }
}
