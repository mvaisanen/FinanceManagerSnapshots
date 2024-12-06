using BlazorFrontendNew.BlazorRedux;
using BlazorFrontendNew.Store;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Redux
{
    public class AppComponent : ReduxComponent<AppState, IAction>
    {
        [Inject] private NavigationManager UriHelper { get; set; }
        protected override void OnInitialized()
        {
            var state = Store.State.LoginState;
            if (string.IsNullOrEmpty(state.JwtToken))
            {
                UriHelper.NavigateTo("/login"); //TODO: Need to make sure using different (?) instance of UriHelper than ReduxComponent wont confuse something
            }

            base.OnInitialized();
        }
    }
}
