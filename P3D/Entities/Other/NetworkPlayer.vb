﻿Public Class NetworkPlayer

    Inherits Entity

    Shared ReadOnly FallbackSkins() As String = {"Birdkeeper", "Artist", "Nurse1", "OldWoman", "OldMan", "Lady", "Lass", "Doctor", "Fisherman", "Captain", "Cameraman", "BlueShirtGuy", "Breeder_Female", "Breeder_Male", "BlackBelt", "BattleGirl", "AromaLady", "Youngster", "YoungCouple_Female", "YoungCouple_Male", "SuperNerd", "SchoolGirl", "SchoolKid", "RichBoy", "Smasher", "Jogger", "Rancher", "Cowgirl", "Picknicker", "Camper", "Gentleman", "PinkShirtGirl", "BugCatcher"}
    Shared FallBack As New Dictionary(Of Integer, String)

    Public Name As String = ""

    ''' <summary>
    ''' The Network ID of the player
    ''' </summary>
    Public NetworkID As Integer = 0

    Public faceRotation As Integer
    Public MapFile As String = ""

    Dim GameJoltID As String = ""
    Dim RotatedSprite As Boolean = False

    Public TextureID As String
    Public SkinSuffix As String
    Public Texture As Texture2D

    Public moving As Boolean = False
    Dim lastRectangle As New Rectangle(0, 0, 0, 0)
    Dim AnimationX As Integer = 1
    Const AnimationDelayLength As Single = 1.1F
    Dim AnimationDelay As Single = AnimationDelayLength

    Dim NameTexture As Texture2D

    Dim LastName As String = ""

    Dim LevelFile As String = ""

    Public BusyType As String = "0"

    Private DownloadingSprite As Boolean = False
    Private CheckForOnlineSprite As Boolean = False

    Public Sub New(ByVal X As Single, ByVal Y As Single, ByVal Z As Single, ByVal Textures() As Texture2D, ByVal TextureID As String, ByVal Rotation As Integer, ByVal ActionValue As Integer, ByVal AdditionalValue As String, ByVal Scale As Vector3, ByVal Name As String, ByVal ID As Integer)
        MyBase.New(X, Y, Z, "NetworkPlayer", Textures, {0, 0}, True, Rotation, Scale, BaseModel.BillModel, 0, "", New Vector3(1.0F))

        Me.Name = Name
        Me.NetworkID = ID
        Me.faceRotation = Rotation
        Me.TextureID = TextureID
        Me.Collision = False
        Me.NeedsUpdate = True

        AssignFallback(ID)

        SetTexture(TextureID)
        ChangeTexture()
        Me.CreateWorldEveryFrame = True

        Me.DropUpdateUnlessDrawn = False
    End Sub

    Private Sub AssignFallback(ByVal ID As Integer)
        If FallBack.ContainsKey(ID) = False Then
            FallBack.Add(ID, FallbackSkins(Core.Random.Next(0, FallbackSkins.Length)))
        End If
    End Sub

    Public Sub SetTexture(ByVal TextureID As String, Optional ByVal SkinSuffix As String = "")
        Me.TextureID = TextureID
        Me.SkinSuffix = SkinSuffix

        Dim texturePath As String = GetTexturePath(TextureID & SkinSuffix)

        Dim OnlineSprite As Texture2D = Nothing
        If Me.GameJoltID <> "" Then
            If GameJolt.Emblem.HasDownloadedSprite(Me.GameJoltID, Me.SkinSuffix) = True Then
                OnlineSprite = GameJolt.Emblem.GetOnlineSprite(Me.GameJoltID, Me.SkinSuffix)
            Else
                Dim t As New Threading.Thread(AddressOf DownloadOnlineSprite)
                t.IsBackground = True
                t.Start()
                DownloadingSprite = True
            End If
        End If

        If OnlineSprite IsNot Nothing Then
            Me.Texture = OnlineSprite
        Else
            If TextureManager.TextureExist(texturePath) = True Then
                Logger.Debug("Change network texture to [" & texturePath & "]")

                Me.Texture = TextureManager.GetTexture(texturePath)
            Else
                Logger.Debug("Texture fallback!")
                Me.Texture = TextureManager.GetTexture("Textures\OverworldSprites\" & FallBack(Me.NetworkID))
            End If
        End If
    End Sub

    Private Sub DownloadOnlineSprite()
        Dim t As Texture2D = GameJolt.Emblem.GetOnlineSprite(Me.GameJoltID, Me.SkinSuffix)

        If t IsNot Nothing Then
            Me.Texture = t
        End If
    End Sub

    Public Shared Function GetTexturePath(ByVal TextureID As String) As String
        Dim texturePath As String = "Textures\OverworldSprites\"
		Dim isPokemon As Boolean = False
		If TextureManager.TextureExist(texturePath & "PlayerSkins\" & TextureID) Then
			texturePath = "Textures\OverworldSprites\PlayerSkins\"
		End If
		If TextureID.StartsWith("[POKEMON|N]") = True Or TextureID.StartsWith("[Pokémon|N]") = True Then
            TextureID = TextureID.Remove(0, 11)
            isPokemon = True
            texturePath = "Pokemon\Overworld\Normal\"
        ElseIf TextureID.StartsWith("[POKEMON|S]") = True Or TextureID.StartsWith("[Pokémon|S]") = True Then
            TextureID = TextureID.Remove(0, 11)
            isPokemon = True
            texturePath = "Pokemon\Overworld\Shiny\"
        End If
        Return texturePath & TextureID
    End Function

    Private Sub ChangeTexture()
        If Not Me.Texture Is Nothing Then
            Dim r As New Rectangle(0, 0, 0, 0)
            Dim cameraRotation As Integer = Screen.Camera.GetFacingDirection()
            Dim spriteIndex As Integer = Me.faceRotation - cameraRotation

            spriteIndex = Me.faceRotation - cameraRotation
            If spriteIndex < 0 Then
                spriteIndex += 4
            End If

            If RotatedSprite = True Then
                Select Case spriteIndex
                    Case 1
                        spriteIndex = 3
                    Case 3
                        spriteIndex = 1
                End Select
            End If

            Dim spriteSize As New Size(CInt(Me.Texture.Width / 3), CInt(Me.Texture.Height / 4))

            If Me.Texture.Width = Me.Texture.Height / 2 Then
                spriteSize = New Size(CInt(Me.Texture.Width / 2), CInt(Me.Texture.Height / 4))
            End If

            Dim x As Integer = 0
            If Me.moving = True Then
                x = GetAnimationX() * spriteSize.Width
            End If

            r = New Rectangle(x, spriteSize.Height * spriteIndex, spriteSize.Width, spriteSize.Height)

            If r <> lastRectangle Then
                lastRectangle = r

                Textures(0) = TextureManager.GetTexture(Me.Texture, r, 1)
            End If
        End If
    End Sub

    Private Function GetAnimationX() As Integer
        If Me.Texture.Width = Me.Texture.Height / 2 Then
            Select Case AnimationX
                Case 1
                    Return 0
                Case 2
                    Return 1
                Case 3
                    Return 0
                Case 4
                    Return 1
            End Select
        Else
            Select Case AnimationX
                Case 1
                    Return 0
                Case 2
                    Return 1
                Case 3
                    Return 0
                Case 4
                    Return 2
            End Select
        End If
        Return 0
    End Function

    Private Sub Move()
        If Me.moving = True Then
            Me.AnimationDelay -= 0.1F
            If Me.AnimationDelay <= 0.0F Then
                Me.AnimationDelay = AnimationDelayLength
                AnimationX += 1
                If AnimationX > 4 Then
                    AnimationX = 1
                End If
            End If
        End If
    End Sub

    Protected Overrides Function CalculateCameraDistance(CPosition As Vector3) as Single
        Return MyBase.CalculateCameraDistance(CPosition) - 0.2f
    End Function

    Public Overrides Sub UpdateEntity()
        If Me.Rotation.Y <> Screen.Camera.Yaw Then
            Me.Rotation.Y = Screen.Camera.Yaw
        End If
        If Not Me.TextureID Is Nothing AndAlso Me.TextureID.ToLower() = "nilllzz" And Me.GameJoltID = "17441" Then
            Me.Rotation.Z = MathHelper.Pi
            RotatedSprite = True
        Else
            RotatedSprite = False
            Me.Rotation.Z = 0
        End If

        ChangeTexture()

        MyBase.UpdateEntity()
    End Sub

    Public Overrides Sub Update()
        If Me.Name <> Me.LastName Then
            Me.LastName = Me.Name
            Me.NameTexture = SpriteFontTextToTexture(FontManager.MainFontWhite, Me.Name)
        End If

        Move()

        If DownloadingSprite AndAlso GameJolt.Emblem.HasDownloadedSprite(GameJoltID) Then
            SetTexture(TextureID)
            ChangeTexture()
            DownloadingSprite = False
        End If

        MyBase.Update()
    End Sub

    Public Overrides Sub Render()
        If ConnectScreen.Connected = True Then
            If IsCorrectScreen() = True Then
                Me.Draw(Me.Model, Textures, False)
                If Core.GameOptions.ShowGUI = True Then
                    If Me.NameTexture IsNot Nothing Then
                        Dim state = GraphicsDevice.DepthStencilState
                        GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead
                        Draw(BaseModel.BillModel, {Me.NameTexture}, False)
                        GraphicsDevice.DepthStencilState = state
                    End If
                    If Me.BusyType <> "0" Then
                        RenderBubble()
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub RenderBubble()
        Dim b As MessageBulb = Nothing
        Select Case BusyType
            Case "1"
                b = New MessageBulb(New Vector3(Me.Position.X, Me.Position.Y + 1, Me.Position.Z), MessageBulb.NotificationTypes.Battle)
            Case "2"
                b = New MessageBulb(New Vector3(Me.Position.X, Me.Position.Y + 1, Me.Position.Z), MessageBulb.NotificationTypes.Waiting)
            Case "3"
                b = New MessageBulb(New Vector3(Me.Position.X, Me.Position.Y + 1, Me.Position.Z), MessageBulb.NotificationTypes.AFK)
        End Select
        If Not b Is Nothing Then
            b.Visible = Me.Visible
            b.Render()
        End If
    End Sub

    Private Function IsCorrectScreen() As Boolean
        Dim screens() As Screen.Identifications = {Screen.Identifications.BattleCatchScreen, Screen.Identifications.MainMenuScreen, Screen.Identifications.BattleGrowStatsScreen, Screen.Identifications.BattleScreen, Screen.Identifications.CreditsScreen, Screen.Identifications.BattleAnimationScreen, Screen.Identifications.ViewModelScreen, Screen.Identifications.HallofFameScreen}
        If screens.Contains(Core.CurrentScreen.Identification) = True Then
            Return False
        Else
            If Core.CurrentScreen.Identification = Screen.Identifications.TransitionScreen Then
                If screens.Contains(CType(Core.CurrentScreen, TransitionScreen).OldScreen.Identification) = True Or screens.Contains(CType(Core.CurrentScreen, TransitionScreen).NewScreen.Identification) = True Then
                    Return False
                End If
            End If
        End If
        Return True
    End Function

    Public Sub ApplyPlayerData(ByVal p As Servers.Player)
        Try
            Me.NetworkID = p.ServersID

            Me.Position = p.Position

            Me.Name = p.Name

            If Not p.Skin.StartsWith("[POKEMON|N]") AndAlso Not p.Skin.StartsWith("[Pokémon|N]") AndAlso Not p.Skin.StartsWith("[POKEMON|S]") AndAlso Not p.Skin.StartsWith("[Pokémon|S]") Then
                If Not String.IsNullOrWhiteSpace(GameJoltID) AndAlso CheckForOnlineSprite = False Then
                    CheckForOnlineSprite = True
                    Me.SetTexture(p.Skin)
                End If
            End If

            If Me.TextureID <> p.Skin Then
                Me.SetTexture(p.Skin)
            End If
            Me.ChangeTexture()

            Me.GameJoltID = p.GameJoltId
            Me.faceRotation = p.Facing
            Me.FaceDirection = p.Facing
            Me.moving = p.Moving
            Me.LevelFile = p.LevelFile
            Me.BusyType = p.BusyType.ToString()
            Me.Visible = False

            If Screen.Level.LevelFile.ToLower() = p.LevelFile.ToLower() Then
                Me.Visible = True
            Else
                If LevelLoader.LoadedOffsetMapNames.Contains(p.LevelFile) = True Then
                    Offset = LevelLoader.LoadedOffsetMapOffsets(LevelLoader.LoadedOffsetMapNames.IndexOf(p.LevelFile))
                    Me.Position.X += Offset.X
                    Me.Position.Y += Offset.Y
                    Me.Position.Z += Offset.Z
                    Me.Visible = True
                End If
            End If
        Catch ex As Exception
            Logger.Debug("NetworkPlayer.vb: Error while assigning player data over network: " & ex.Message)
        End Try
    End Sub

    Public Overrides Sub ClickFunction()
        Dim Data(4) As Object
        Data(0) = Me.NetworkID
        Data(1) = Me.GameJoltID
        Data(2) = Me.Name
        Data(3) = Me.Texture

        'Basic.SetScreen(New GameJolt.PhoneScreen(Basic.currentScreen, GameJolt.PhoneScreen.EntryModes.DisplayUser, Data))
    End Sub

    Public Shared Sub ScreenRegionChanged()
        If Not Core.CurrentScreen Is Nothing AndAlso Not Screen.Level Is Nothing Then
            For Each netPlayer As NetworkPlayer In Screen.Level.NetworkPlayers
                netPlayer.LastName = ""
            Next
        End If
    End Sub

    Shared SpriteTextStorage As New Dictionary(Of String, Texture2D)

    Private Shared Function SpriteFontTextToTexture(ByVal font As SpriteFont, ByVal text As String) As Texture2D
        If text.Length > 0 Then
            If SpriteTextStorage.ContainsKey(text) = True Then
                Return SpriteTextStorage(text)
            Else
                Dim size As Vector2 = font.MeasureString(text)
                Dim renderTarget As RenderTarget2D = New RenderTarget2D(Core.GraphicsDevice, CInt(size.X), CInt(size.Y * 3))
                Core.GraphicsDevice.SetRenderTarget(renderTarget)

                Core.GraphicsDevice.Clear(Color.Transparent)

                Core.SpriteBatch.Begin()
                Canvas.DrawRectangle(New Rectangle(0, 0, CInt(size.X), CInt(size.Y)), New Color(0, 0, 0, 150))
                Core.SpriteBatch.DrawString(font, text, Vector2.Zero, Color.White)
                Core.SpriteBatch.End()

                Core.GraphicsDevice.SetRenderTarget(Nothing)
                SpriteTextStorage.Add(text, renderTarget)

                Return renderTarget
            End If
        End If
        Return Nothing
    End Function

End Class