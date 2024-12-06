namespace AdminFrontend.Blazor.Store.LoginUseCase
{
    //public class LoginSucceededAction
    //{
    //    public string Jwt { get; }
    //    public LoginSucceededAction(string jwt)
    //    {
    //        Jwt = jwt;
    //    }
    //}

    public record LoginSucceededAction(string jwt);
    
}
