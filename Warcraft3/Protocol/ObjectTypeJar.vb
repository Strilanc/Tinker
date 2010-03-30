Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class ObjectTypeJar
        Inherits BaseJar(Of UInt32)

        Private Shared ReadOnly DataJar As New UInt32Jar()

        Public Overrides Function Pack(Of TValue As UInt32)(ByVal value As TValue) As IPickle(Of TValue)
            Return DataJar.Pack(value).With(jar:=Me, description:=Function() GameActions.TypeIdString(value))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt32)
            Dim pickle = DataJar.Parse(data)
            Return pickle.With(jar:=Me, description:=Function() GameActions.TypeIdString(pickle.Value))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt32)
            Dim control = New TextBox()
            control.Text = "hpea"
            Return New DelegatedValueEditor(Of UInt32)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Dim bytes = control.Text.ToAscBytes.Reverse
                            If bytes.Count = 4 AndAlso (From b In bytes Where b < 32 Or b >= 128).None Then
                                Return control.Text.ToAscBytes.Reverse.ToUInt32
                            Else
                                Try
                                    bytes = (From word In control.Text.Split(" "c)
                                             Where word <> ""
                                             Select CByte(word.FromHexToUInt64(ByteOrder.BigEndian))
                                             ).ToList
                                    If bytes.Count <> 4 Then Throw New ArgumentException("Incorrect number of hex bytes.")
                                    Return (From word In control.Text.Split(" "c)
                                            Where word <> ""
                                            Select CByte(word.FromHexToUInt64(ByteOrder.BigEndian))
                                            ).ToUInt32
                                Catch ex As ArgumentException
                                    Throw New PicklingException("Invalid game object type. Must be in rawcode (eg. hpea) or 4 hex bytes (eg. 00 00 00 00) format.", ex)
                                End Try
                            End If
                        End Function,
                setter:=Sub(value) control.Text = GameActions.TypeIdString(value))
        End Function
    End Class
End Namespace
