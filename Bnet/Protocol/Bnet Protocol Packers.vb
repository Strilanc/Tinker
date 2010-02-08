'Tinker - Warcraft 3 game hosting bot
'Copyright (C) 2010 Craig Gidney
'
'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.
'
'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.
'You should have received a copy of the GNU General Public License
'along with this program.  If not, see http://www.gnu.org/licenses/

Namespace Bnet.Protocol
    Public Module Packers
        <Pure()>
        Public Function MakeAuthenticationBegin(ByVal majorVersion As UInteger,
                                                ByVal localIPAddress As Net.IPAddress) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.ProgramAuthenticationBegin, New Dictionary(Of InvariantString, Object) From {
                    {"protocol", 0},
                    {"platform", "IX86"},
                    {"product", "W3XP"},
                    {"product major version", majorVersion},
                    {"product language", "SUne"},
                    {"internal ip", localIPAddress},
                    {"time zone offset", 240},
                    {"location id", 1033},
                    {"language id", MPQ.LanguageId.English},
                    {"country abrev", "USA"},
                    {"country name", "United States"}
                })
        End Function

        <Pure()>
        Public Function MakeAuthenticationFinish(ByVal version As IReadableList(Of Byte),
                                                 ByVal revisionCheckResponse As UInt32,
                                                 ByVal clientCDKeySalt As UInt32,
                                                 ByVal cdKeyOwner As String,
                                                 ByVal exeInformation As String,
                                                 ByVal productAuthentication As CKL.WC3CredentialPair) As Packet
            Contract.Requires(version IsNot Nothing)
            Contract.Requires(cdKeyOwner IsNot Nothing)
            Contract.Requires(exeInformation IsNot Nothing)
            Contract.Requires(productAuthentication IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return New Packet(ClientPackets.ProgramAuthenticationFinish, New Dictionary(Of InvariantString, Object) From {
                    {"client cd key salt", clientCDKeySalt},
                    {"exe version", version},
                    {"revision check response", revisionCheckResponse},
                    {"# cd keys", 2},
                    {"is spawn", 0},
                    {"ROC cd key", productAuthentication.AuthenticationROC},
                    {"TFT cd key", productAuthentication.AuthenticationTFT},
                    {"exe info", exeInformation},
                    {"owner", cdKeyOwner}
                })
        End Function

        <Pure()>
        Public Function MakeGetFileTime(ByVal fileName As InvariantString,
                                        ByVal requestId As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.GetFileTime, New Dictionary(Of InvariantString, Object) From {
                    {"request id", requestId},
                    {"unknown", 0},
                    {"filename", fileName.ToString}
                })
        End Function

        <Pure()>
        Public Function MakeAccountLogOnBegin(ByVal credentials As ClientCredentials) As Packet
            Contract.Requires(credentials IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.UserAuthenticationBegin, New Dictionary(Of InvariantString, Object) From {
                    {"client public key", credentials.PublicKeyBytes},
                    {"username", credentials.UserName}
                })
        End Function

        <Pure()>
        Public Function MakeAccountLogOnFinish(ByVal clientPasswordProof As IReadableList(Of Byte)) As Packet
            Contract.Requires(clientPasswordProof IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.UserAuthenticationFinish, New Dictionary(Of InvariantString, Object) From {
                    {"client password proof", clientPasswordProof}
                })
        End Function

        <Pure()>
        Public Function MakeEnterChat() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.EnterChat, New Dictionary(Of InvariantString, Object) From {
                    {"username", ""},
                    {"statstring", ""}
                })
        End Function

        <Pure()>
        Public Function MakeNetGamePort(ByVal port As UShort) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim vals As New Dictionary(Of InvariantString, Object)
            vals("port") = port
            Return New Packet(ClientPackets.NetGamePort, vals)
        End Function

        <Pure()>
        Public Function MakeQueryGamesList(Optional ByVal specificGameName As String = "",
                                           Optional ByVal listCount As Integer = 20) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.QueryGamesList, New Dictionary(Of InvariantString, Object) From {
                    {"filter", WC3.Protocol.GameTypes.MaskFilterable},
                    {"filter mask", 0},
                    {"unknown0", 0},
                    {"list count", listCount},
                    {"game name", specificGameName},
                    {"game password", ""},
                    {"game stats", ""}})
        End Function

        <Pure()>
        Public Function MakeJoinChannel(ByVal joinType As JoinChannelType,
                                        ByVal channel As InvariantString) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim vals As New Dictionary(Of InvariantString, Object)
            Return New Packet(ClientPackets.JoinChannel, New Dictionary(Of InvariantString, Object) From {
                    {"join type", joinType},
                    {"channel", channel.ToString}})
        End Function

        <Pure()>
        Public Function MakeCreateGame3(ByVal game As WC3.GameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Const MAX_GAME_NAME_LENGTH As UInteger = 31
            If game.Name.Length > MAX_GAME_NAME_LENGTH Then
                Throw New ArgumentException("Game name must be less than 32 characters long.", "game")
            End If

            Return New Packet(ClientPackets.CreateGame3, New Dictionary(Of InvariantString, Object) From {
                    {"game state", game.GameState},
                    {"seconds since creation", CUInt(game.Age.TotalSeconds)},
                    {"game type", game.GameType},
                    {"unknown1=1023", 1023},
                    {"is ladder", 0},
                    {"name", game.Name.ToString},
                    {"password", ""},
                    {"num free slots", game.TotalSlotCount - game.UsedSlotCount},
                    {"game id", game.GameId},
                    {"statstring", game.GameStats}})
        End Function

        <Pure()>
        Public Function MakeCloseGame3() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.CloseGame3, New Dictionary(Of InvariantString, Object))
        End Function

        <Pure()>
        Public Function MakeChatCommand(ByVal text As String) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            If text.Length > ClientPackets.MaxChatCommandTextLength Then
                Throw New ArgumentException("Text cannot exceed {0} characters.".Frmt(ClientPackets.MaxChatCommandTextLength), "text")
            End If
            Return New Packet(ClientPackets.ChatCommand, New Dictionary(Of InvariantString, Object) From {
                    {"text", text}})
        End Function

        <Pure()>
        Public Function MakePing(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return New Packet(ClientPackets.Ping, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function

        <Pure()>
        Public Function MakeWarden(ByVal encryptedData As IReadableList(Of Byte)) As Packet
            Contract.Requires(encryptedData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return New Packet(ClientPackets.Warden, New Dictionary(Of InvariantString, Object) From {
                    {"encrypted data", encryptedData}})
        End Function
    End Module
End Namespace
