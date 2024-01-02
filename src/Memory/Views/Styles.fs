[<AutoOpen>]
module Memory.Views.Styles

open System
open Fun.Css
open Fun.Blazor

type Reveal =
    static member Name = "reveal"

    static member Keyframes = keyframes Reveal.Name {
        keyframe 0 {
            width 0
            height 0
        }
        keyframe 100 {
            width "unset"
            height "unset"
        }
    }


type FadeInUp =
    static member Name = "fade-in-up"

    static member Keyframes = keyframes FadeInUp.Name {
        keyframe 0 {
            opacity 0
            transformTranslateY 10
        }
        keyframe 100 {
            opacity 100
            transformTranslateY 0
        }
    }


type IScopedCssRules with

    member this.FadeInUpCss(?delay, ?duration) =
        this.IncludeKeyFrame FadeInUp.Keyframes
        css {
            animationName FadeInUp.Name
            animationTimingFunctionEaseIn
            animationFillModeBoth
            animationDelay (TimeSpan.FromMilliseconds(defaultArg delay 0))
            animationDuration (TimeSpan.FromMilliseconds(defaultArg duration 400))
        }

    member this.RevealSpan(char: Char, ?delay, ?duration, ?otherCss, ?attr) =
        this.IncludeKeyFrame Reveal.Keyframes
        span {
            style {
                width 0
                height 0
                displayInlineBlock
                overflowHidden
                whiteSpaceNowrap
                animationName Reveal.Name
                animationFillModeForwards
                animationDelay (TimeSpan.FromMilliseconds(float (defaultArg delay 0)))
                animationDuration (TimeSpan.FromMilliseconds(float (defaultArg duration 0)))
                if char = ' ' then css { paddingRight "0.5em" }
                defaultArg otherCss (Internal.CombineKeyValue(fun x -> x))
            }
            defaultArg attr html.emptyAttr
            string char
        }
