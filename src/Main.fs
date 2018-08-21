module Main

open Elmish

type Model =
    | Redirect of Session
    | NotFound of Session
    | Home of Home.Model
    | Settings of Settings.Model
    | Login of Login.Model
    | Register of Register.Model
    | Profile of Username Profile.Model
    | Article of Article.Model
    | Editor of (Slug option) * Editor.Model

// MODEL

// init : Viewer option -> Url -> Nav.Key -> ( Model, Cmd Msg )
let init maybeViewer url navKey =
    changeRouteTo (Route.fromUrl url)
        (Redirect (Session.fromViewer navKey maybeViewer))

// VIEW


// view : Model -> Document Msg
let view model =
    let viewPage page toMsg config =
        let { title = title; body = body } = Page.view (Session.viewer (toSession model)) page config
        { title = title
         body = List.map (Html.map toMsg) body
        }
    match model with
    | Redirect _ ->
        viewPage Page.Other (fun _ -> Ignored) Blank.view

    | NotFound _ ->
        viewPage Page.Other (fun _ -> Ignored) NotFound.view

    | Settings settings ->
        viewPage Page.Other GotSettingsMsg (Settings.view settings)

    | Home home ->
        viewPage Page.Home GotHomeMsg (Home.view home)

    | Login login ->
        viewPage Page.Other GotLoginMsg (Login.view login)

    | Register register ->
        viewPage Page.Other GotRegisterMsg (Register.view register)

    | Profile username profile ->
        viewPage (Page.Profile username) GotProfileMsg (Profile.view profile)

    | Article article ->
        viewPage Page.Other GotArticleMsg (Article.view article)

    | Editor None editor ->
        viewPage Page.NewArticle GotEditorMsg (Editor.view editor)

    | Editor (Some _) editor ->
        viewPage Page.Other GotEditorMsg (Editor.view editor)

// UPDATE

type Msg =
    | Ignored
    | ChangedRoute of (Route option)
    | ChangedUrl of Url
    | ClickedLink of Browser.UrlRequest
    | GotHomeMsg of Home.Msg
    | GotSettingsMsg of Settings.Msg
    | GotLoginMsg of Login.Msg
    | GotRegisterMsg of Register.Msg
    | GotProfileMsg of Profile.Msg
    | GotArticleMsg of Article.Msg
    | GotEditorMsg of Editor.Msg
    | GotSession of Session

// toSession : Model -> Session
let toSession page =
    match page with
    | Redirect session ->
        session

    | NotFound session ->
        session

    | Home home ->
        Home.toSession home

    | Settings settings ->
        Settings.toSession settings

    | Login login ->
        Login.toSession login

    | Register register ->
        Register.toSession register

    | Profile _ profile ->
        Profile.toSession profile

    | Article article ->
        Article.toSession article

    | Editor _ editor ->
        Editor.toSession editor


// changeRouteTo : Route option -> Model -> ( Model, Cmd Msg )
let changeRouteTo maybeRoute model =
    let session = toSession model
    match maybeRoute with
    | None ->
        ( NotFound session, Cmd.none )

    | Some Route.Root ->
        ( model, Route.replaceUrl (Session.navKey session) Route.Home )

    | Some Route.Logout ->
        ( model, Api.logout )

    | Some Route.NewArticle ->
        Editor.initNew session
            |> updateWith (Editor None) GotEditorMsg model

    | Some (Route.EditArticle slug) ->
        Editor.initEdit session slug
            |> updateWith (Editor (Some slug)) GotEditorMsg model

    | Some Route.Settings ->
        Settings.init session
            |> updateWith Settings GotSettingsMsg model

    | Some Route.Home ->
        Home.init session
            |> updateWith Home GotHomeMsg model

    | Some Route.Login ->
        Login.init session
            |> updateWith Login GotLoginMsg model

    | Some Route.Register ->
        Register.init session
            |> updateWith Register GotRegisterMsg model

    | Some (Route.Profile username) ->
        Profile.init session username
            |> updateWith (Profile username) GotProfileMsg model

    | Some (Route.Article slug) ->
        Article.init session slug
            |> updateWith Article GotArticleMsg model


// update : Msg -> Model -> ( Model, Cmd Msg )
let update msg model =
    match ( msg, model ) with
    | ( Ignored, _ ) ->
        ( model, Cmd.none )

    | ( ClickedLink urlRequest, _ ) ->
        match urlRequest with
        | Browser.Internal url ->
            match url.fragment with
            | None ->
                // If we got a link that didn't include a fragment,
                // it's from one of those (href "") attributes that
                // we have to include to make the RealWorld CSS work.
                //
                // In an application doing path routing instead of
                // fragment-based routing, this entire
                // `match url.fragment with` expression this comment
                // is inside would be unnecessary.
                ( model, Cmd.none )

            | Some _ ->
                ( model
                , Nav.pushUrl (Session.navKey (toSession model)) (Url.toString url)
                )

        | Browser.External href ->
            ( model
            , Nav.load href
            )

    | ( ChangedUrl url, _ ) ->
        changeRouteTo (Route.fromUrl url) model

    | ( ChangedRoute route, _ ) ->
        changeRouteTo route model

    | ( GotSettingsMsg subMsg, Settings settings ) ->
        Settings.update subMsg settings
            |> updateWith Settings GotSettingsMsg model

    | ( GotLoginMsg subMsg, Login login ) ->
        Login.update subMsg login
            |> updateWith Login GotLoginMsg model

    | ( GotRegisterMsg subMsg, Register register ) ->
        Register.update subMsg register
            |> updateWith Register GotRegisterMsg model

    | ( GotHomeMsg subMsg, Home home ) ->
        Home.update subMsg home
            |> updateWith Home GotHomeMsg model

    | ( GotProfileMsg subMsg, Profile username profile ) ->
        Profile.update subMsg profile
            |> updateWith (Profile username) GotProfileMsg model

    | ( GotArticleMsg subMsg, Article article ) ->
        Article.update subMsg article
            |> updateWith Article GotArticleMsg model

    | ( GotEditorMsg subMsg, Editor slug editor ) ->
        Editor.update subMsg editor
            |> updateWith (Editor slug) GotEditorMsg model

    | ( GotSession session, Redirect _ ) ->
        ( Redirect session
        , Route.replaceUrl (Session.navKey session) Route.Home
        )

    | ( _, _ ) ->
        // Disregard messages that arrived for the wrong page.
        ( model, Cmd.none )


// updateWith : (subModel -> Model) -> (subMsg -> Msg) -> Model -> ( subModel, Cmd subMsg ) -> ( Model, Cmd Msg )
let updateWith toModel toMsg model ( subModel, subCmd ) =
    ( toModel subModel
    , Cmd.map toMsg subCmd
    )

// SUBSCRIPTIONS

// subscriptions : Model -> Sub Msg
let subscriptions model =
    match model with
    | NotFound _ ->
        Sub.none

    | Redirect _ ->
        Session.changes GotSession (Session.navKey (toSession model))

    | Settings settings ->
        Sub.map GotSettingsMsg (Settings.subscriptions settings)

    | Home home ->
        Sub.map GotHomeMsg (Home.subscriptions home)

    | Login login ->
        Sub.map GotLoginMsg (Login.subscriptions login)

    | Register register ->
        Sub.map GotRegisterMsg (Register.subscriptions register)

    | Profile _ profile ->
        Sub.map GotProfileMsg (Profile.subscriptions profile)

    | Article article ->
        Sub.map GotArticleMsg (Article.subscriptions article)

    | Editor _ editor ->
        Sub.map GotEditorMsg (Editor.subscriptions editor)


// MAIN

open Elmish.React
open Elmish.Browser
open Elmish.HMR

Program.mkProgram init update view
|> Program.toNavigable (parseHash Router.pageParser) urlUpdate
#if DEBUG
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
|> Program.run
