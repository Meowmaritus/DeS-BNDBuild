
Public Structure GameDef

    Public ReadOnly Property SourceRoot As String 'DeS: "N:\DemonsSoul\", DaS: "N:\FRPG\", etc
    Public ReadOnly Property DvdRoot As String 'DeS: "DVDROOT", DaS PC: "INTERROOT_win32", etc
    Public ReadOnly Property DisplayName As String 'DeS: "Demon's Souls", DaS PC: "Dark Souls (PC)"
    Public ReadOnly Property DataRoot As String 'DeS: "USRDIR", DaS PC: "DATA"

    Public Shared DemonsSoul As GameDef
    Public Shared FRPG_win32 As GameDef

    Shared Sub New()

        DemonsSoul = New GameDef() With {
            ._SourceRoot = "N:\DemonsSoul\",
            ._DvdRoot = "DVDROOT",
            ._DisplayName = "Demon's Souls",
            ._DataRoot = "USRDIR"
        }

        FRPG_win32 = New GameDef() With {
            ._SourceRoot = "N:\FRPG\",
            ._DvdRoot = "INTERROOT_win32",
            ._DisplayName = "Dark Souls (PC)",
            ._DataRoot = "DATA"
        }

    End Sub

End Structure
