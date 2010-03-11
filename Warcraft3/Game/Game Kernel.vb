''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Namespace WC3
    Public Enum GameState
        AcceptingPlayers = 0
        PreCounting = 1
        CountingDown = 2
        Loading = 3
        Playing = 4
        Disposed = 5
    End Enum

    Public NotInheritable Class GameKernel
        Inherits DisposableWithTask

        Private _state As GameState = GameState.AcceptingPlayers
        Private ReadOnly _players As New AsyncViewableCollection(Of Player)
        Private ReadOnly _clock As IClock

        Public Event ChangedState(ByVal sender As GameKernel, ByVal oldState As GameState, ByVal newState As GameState)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_players IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
        End Sub

        Public Sub New(ByVal clock As IClock)
            Contract.Assume(clock IsNot Nothing)
            Me._clock = clock
        End Sub

        Public ReadOnly Property Clock As IClock
            Get
                Contract.Ensures(Contract.Result(Of IClock)() IsNot Nothing)
                Return _clock
            End Get
        End Property
        Public ReadOnly Property Players As AsyncViewableCollection(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of AsyncViewableCollection(Of Player))() IsNot Nothing)
                Return _players
            End Get
        End Property
        Public Property State As GameState
            Get
                Return _state
            End Get
            Set(ByVal value As GameState)
                Dim oldState = State
                _state = value
                RaiseEvent ChangedState(Me, oldState, value)
            End Set
        End Property
    End Class
End Namespace
