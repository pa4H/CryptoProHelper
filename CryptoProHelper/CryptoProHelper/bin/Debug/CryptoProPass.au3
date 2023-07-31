#NoTrayIcon
$passFile = FileOpen(@ScriptDir & '\autoPass.txt', 0)
$pass = FileRead($passFile)
;MsgBox(4096, $pass, @ScriptDir)
While 1
$aList = WinList("КриптоПро CSP")
$window = "";
For $i = 1 To $aList[0][0]
    If ControlGetText ($aList[$i][1], "", "Static2") = "Пароль:" Then
		$window = $aList[$i][1]
		ConsoleWrite($window & @CRLF)
		WinWait($window)
		ControlSetText ($window, "", "Edit1", $pass)
		ControlClick($window, "", "Button1")
		ControlClick($window, "", "Button2")
    EndIf
Next
Sleep(100)
WEnd