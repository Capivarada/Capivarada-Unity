mergeInto(LibraryManager.library, {

    GoogleLogin: function (clientIdPtr, titleIdPtr) {

        const clientId = UTF8ToString(clientIdPtr);
        const titleId = UTF8ToString(titleIdPtr);

        console.log("[Google] Iniciando login...");

        if (!window.google || !window.google.accounts) {
            console.error(
                "[Google] Google Identity Services não foi carregado."
            );

            SendUnityError(
                "Google Identity Services não foi carregado."
            );

            return;
        }

        if (!window.PlayFabClientSDK) {
            console.error(
                "[PlayFab] PlayFab JavaScript SDK não foi carregado."
            );

            SendUnityError(
                "PlayFab JavaScript SDK não foi carregado."
            );

            return;
        }

        const client =
            google.accounts.oauth2.initTokenClient({

                client_id: clientId,

                scope:
                    "https://www.googleapis.com/auth/userinfo.profile " +
                    "https://www.googleapis.com/auth/userinfo.email",

                callback: function (tokenResponse) {

                    if (tokenResponse.error) {

                        console.error(
                            "[Google] Erro:",
                            tokenResponse
                        );

                        SendUnityError(
                            JSON.stringify(tokenResponse)
                        );

                        return;
                    }

                    console.log(
                        "[Google] Access Token recebido."
                    );

                    PlayFabClientSDK.LoginWithGoogleAccount(
                        {
                            AccessToken:
                                tokenResponse.access_token,

                            CreateAccount: true,

                            TitleId: titleId
                        },

                        function (response, error) {

                            if (response) {

                                console.log(
                                    "[PlayFab] Login Google OK."
                                );

                                const result = {

                                    PlayFabId:
                                        response.PlayFabId,

                                    NewlyCreated:
                                        response.NewlyCreated

                                };

                                SendUnitySuccess(
                                    JSON.stringify(result)
                                );

                            } else {

                                console.error(
                                    "[PlayFab] Erro:",
                                    error
                                );

                                SendUnityError(
                                    JSON.stringify(error)
                                );
                            }
                        }
                    );
                }
            });

        client.requestAccessToken();
    }
});


function SendUnitySuccess(json) {

    if (
        window.unityInstance &&
        window.unityInstance.SendMessage
    ) {

        window.unityInstance.SendMessage(
            "GoogleAuthManager",
            "OnPlayFabGoogleLoginSuccess",
            json
        );

    } else {

        console.error(
            "[Unity] unityInstance não encontrado."
        );
    }
}


function SendUnityError(json) {

    if (
        window.unityInstance &&
        window.unityInstance.SendMessage
    ) {

        window.unityInstance.SendMessage(
            "GoogleAuthManager",
            "OnPlayFabGoogleLoginError",
            json
        );

    }
}
