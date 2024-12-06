using Fluxor;

namespace AdminFrontend.Blazor.Store.LoginUseCase
{
    [FeatureState]
    public class LoginState
    {
        public string? JwtToken { get; }

        private LoginState() { }

        public LoginState(string jwtToken)
        {
            JwtToken = jwtToken;
        }
    }
}
