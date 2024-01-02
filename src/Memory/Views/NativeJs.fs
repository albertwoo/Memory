[<AutoOpen>]
module Memory.Views.NativeJsExtensions

open Fun.Htmx


[<AutoOpen>]
module JsUtils =
    /// Used for VSCode for syntax highlight
    let js (x: string) = x


type NativeJs with

    static member AppendFileCreationHiddenFields(container: string, target: string) =
        js
            $$"""
            (function(){
                const creationTimeClassName = 'creation-time';
                const containerElt = document.querySelector('{{container}}');
                const filesElt = document.querySelector('{{target}}');
                filesElt.addEventListener('change', () => {
                    // Clear old
                    document.querySelectorAll(`{{container}} .${creationTimeClassName}`).forEach(x => x.remove());
                    // Add new
                    const files = filesElt.files;
                    for (var i = 0; i < files.length; i++) {
                        const hiddenElt = document.createElement('input');
                        hiddenElt.type = 'hidden';
                        hiddenElt.name = files[i].name;
                        hiddenElt.value = files[i].lastModified;
                        hiddenElt.classList.add(creationTimeClassName);
                        containerElt.appendChild(hiddenElt);
                    }
                });
            })();
            """