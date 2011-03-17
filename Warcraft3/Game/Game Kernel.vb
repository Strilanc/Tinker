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

        Private ReadOnly _logger As Logger
        Private ReadOnly _clock As IClock
        Private ReadOnly _inQueue As CallQueue
        Private ReadOnly _outQueue As CallQueue
        Private ReadOnly _players As New ObservableCollection(Of Player)
        Private _state As GameState = GameState.AcceptingPlayers

        Public Event ChangedState(sender As GameKernel, oldState As GameState, newState As GameState)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_inQueue IsNot Nothing)
            Contract.Invariant(_outQueue IsNot Nothing)
            Contract.Invariant(_players IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
        End Sub

        Public Sub New(clock As IClock,
                       inQueue As CallQueue,
                       outQueue As CallQueue,
                       logger As Logger)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(inQueue IsNot Nothing)
            Contract.Assume(outQueue IsNot Nothing)
            Contract.Assume(logger IsNot Nothing)
            Me._clock = clock
            Me._inQueue = inQueue
            Me._outQueue = outQueue
            Me._logger = logger
        End Sub

        Public ReadOnly Property InQueue As CallQueue
            Get
                Contract.Ensures(Contract.Result(Of CallQueue)() IsNot Nothing)
                Return _inQueue
            End Get
        End Property
        Public ReadOnly Property OutQueue As CallQueue
            Get
                Contract.Ensures(Contract.Result(Of CallQueue)() IsNot Nothing)
                Return _outQueue
            End Get
        End Property
        Public ReadOnly Property Clock As IClock
            Get
                Contract.Ensures(Contract.Result(Of IClock)() IsNot Nothing)
                Return _clock
            End Get
        End Property
        Public ReadOnly Property Logger As Logger
            Get
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Players As ObservableCollection(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of ObservableCollection(Of Player))() IsNot Nothing)
                Return _players
            End Get
        End Property
        Public Property State As GameState
            Get
                Return _state
            End Get
            Set(value As GameState)
                Dim oldState = State
                _state = value
                RaiseEvent ChangedState(Me, oldState, value)
            End Set
        End Property
    End Class
End Namespace
