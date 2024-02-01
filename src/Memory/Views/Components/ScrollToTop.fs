namespace Memory.Views.Components

open Fun.Blazor
open Memory.Views

type ScrollToTop =

    static member Btn() = button {
        class' "btn btn-circle btn-primary shadow-md opacity-70 hover:opacity-100"
        on.click "window.scrollTo({top: 0, behavior: 'smooth'});"
        Icons.DoubleUp()
    }
