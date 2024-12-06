using AdminFrontend.Blazor.Store.LoginUseCase;
using Fluxor;

namespace AdminFrontend.Blazor.Store
{
    public static class Reducers
    {
        [ReducerMethod]
        public static LoginState ReduceLoginAction(LoginState state, LoginSucceededAction action) =>
            new LoginState(jwtToken: action.jwt);
    }
}
