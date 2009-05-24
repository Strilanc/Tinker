Namespace Warcraft3
    Partial Public NotInheritable Class W3Game
        Public MustInherit Class W3GamePart
            Implements IW3GamePart

            Protected ReadOnly game As W3Game
            Public Sub New(ByVal body As W3Game)
                Me.game = body
            End Sub

            Private ReadOnly Property _game() As IW3Game Implements IW3GamePart.game
                Get
                    Return game
                End Get
            End Property
        End Class
    End Class
End Namespace
