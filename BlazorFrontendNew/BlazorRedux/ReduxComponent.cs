using System;
using Microsoft.AspNetCore.Components.WebAssembly;
//using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorFrontendNew.BlazorRedux
{
    public class ReduxComponent<TState, TAction> : ComponentBase, IDisposable
    {
        [Inject] public Store<TState, TAction> Store { get; set; }
        [Inject] private NavigationManager UriHelper { get; set; }

        public TState State => Store.State;

        public RenderFragment ReduxDevTools;

        public void Dispose()
        {
            Store.Change -= OnChangeHandler;
        }

        protected override void OnInitialized()
        {
            Store.Init(UriHelper);
            Store.Change += OnChangeHandler;

            ReduxDevTools = builder =>
            {
                var seq = 0;
                builder.OpenComponent<ReduxDevTools>(seq);
                builder.CloseComponent();
            };
        }

        private void OnChangeHandler(object sender, EventArgs e)
        {
            //TODO: Miksi logoutissa jwt toekn jää sivulle? Tueeko tämä liian aikaisin, myöhään?
            Console.WriteLine("ReduxComponent: StateHasChanged()");
            StateHasChanged();
        }

        public void Dispatch(TAction action)
        {
            Store.Dispatch(action);
        }
    }
}