Unicode true
RequestExecutionLevel user
SetCompressor /SOLID zlib

!ifndef APP_VERSION
  !define APP_VERSION "1.0.0-preview.1"
!endif
!ifndef APP_VERSION_NUMERIC
  !define APP_VERSION_NUMERIC "1.0.0.0"
!endif
!ifndef APP_PAYLOAD_DIR
  !define APP_PAYLOAD_DIR ".\windows-publish"
!endif
!ifndef APP_SETUP_OUT
  !define APP_SETUP_OUT ".\RGAA-Studio-Setup.exe"
!endif
!ifndef APP_HEADER_BITMAP
  !define APP_HEADER_BITMAP ".\installer-header.bmp"
!endif
!ifndef APP_ICON
  !define APP_ICON ".\RgaaStudio.ico"
!endif
!ifndef APP_LICENSE_FILE
  !define APP_LICENSE_FILE ".\LICENSE"
!endif
!ifndef APP_WEBVIEW2_NOTICE
  !define APP_WEBVIEW2_NOTICE ".\WEBVIEW2-RUNTIME-NOTICE.txt"
!endif
!ifndef APP_WEBVIEW2_INSTALLER
  !define APP_WEBVIEW2_INSTALLER ".\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"
!endif

!include "MUI2.nsh"
!include "LogicLib.nsh"

Var NeedsWebView2
Var WebView2ExitCode

Name "RGAA Studio ${APP_VERSION}"
OutFile "${APP_SETUP_OUT}"
InstallDir "$LOCALAPPDATA\Programs\RGAA Studio"
InstallDirRegKey HKCU "Software\RGAAStudio" "InstallDir"
ShowInstDetails nevershow
ShowUninstDetails nevershow
BrandingText "RGAA Studio  ·  Auditer, comprendre, consigner"

VIProductVersion "${APP_VERSION_NUMERIC}"
VIAddVersionKey "ProductName" "RGAA Studio"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "FileDescription" "Installateur RGAA Studio"
VIAddVersionKey "FileVersion" "${APP_VERSION_NUMERIC}"
VIAddVersionKey "CompanyName" "numera11y"
VIAddVersionKey "LegalCopyright" "Copyright (c) 2026 numera11y"

!define MUI_BGCOLOR "EEF2FA"
!define MUI_TEXTCOLOR "171F3B"
!define MUI_ICON "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "${APP_HEADER_BITMAP}"
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP_NOSTRETCH
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "Installer RGAA Studio"
!define MUI_WELCOMEPAGE_TEXT "Cet assistant installe ou met à jour RGAA Studio ${APP_VERSION} pour votre compte Windows.$\r$\n$\r$\nSi WebView2 est absent, le runtime Evergreen hors ligne fourni avec l’assistant sera installé.$\r$\n$\r$\nVos dossiers d’audit sont préservés lors des mises à jour et désinstallations."
!define MUI_LICENSEPAGE_TEXT_TOP "RGAA Studio est distribué sous licence MIT."
!define MUI_DIRECTORYPAGE_TEXT_TOP "Choisissez le dossier de l’application. Si une version précédente est installée, son emplacement sera proposé."
!define MUI_COMPONENTSPAGE_TEXT_TOP "Choisissez la création d’un raccourci sur le Bureau. Le raccourci du menu Démarrer sera créé dans tous les cas."
!define MUI_FINISHPAGE_TITLE "RGAA Studio est prêt"
!define MUI_FINISHPAGE_TEXT "L’installation de RGAA Studio ${APP_VERSION} est terminée.$\r$\nLes données de vos audits restent dans votre profil utilisateur."
!define MUI_FINISHPAGE_RUN "$INSTDIR\RgaaStudio.Desktop.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Lancer RGAA Studio"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${APP_LICENSE_FILE}"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "French"

Function DetectWebView2
  StrCpy $R1 ""
  SetRegView 64
  ReadRegStr $R1 HKLM "SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${If} $R1 == ""
    ReadRegStr $R1 HKCU "SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${EndIf}
  ${If} $R1 == ""
    ReadRegStr $R1 HKLM "SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${EndIf}
  SetRegView 64
  ${If} $R1 == "0.0.0.0"
    StrCpy $R1 ""
  ${EndIf}
FunctionEnd

Function .onInit
  SetShellVarContext current
  FindWindow $R0 "" "RGAA Studio"
  ${If} $R0 != 0
    MessageBox MB_ICONEXCLAMATION|MB_OK "Fermez RGAA Studio avant de lancer l’installation ou la mise à jour."
    Abort
  ${EndIf}
  Call DetectWebView2
  StrCpy $NeedsWebView2 "0"
  ${If} $R1 == ""
    StrCpy $NeedsWebView2 "1"
  ${EndIf}
FunctionEnd

Section "Application RGAA Studio (obligatoire)" SecApplication
  SectionIn RO
  ${If} $NeedsWebView2 == "1"
    SetOutPath "$PLUGINSDIR"
    File /oname=MicrosoftEdgeWebView2RuntimeInstallerX64.exe "${APP_WEBVIEW2_INSTALLER}"
    DetailPrint "Installation du runtime Microsoft Edge WebView2 Evergreen…"
    ExecWait "$\"$PLUGINSDIR\MicrosoftEdgeWebView2RuntimeInstallerX64.exe$\" /silent /install" $WebView2ExitCode
    Delete "$PLUGINSDIR\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"
    ${If} $WebView2ExitCode != 0
    ${AndIf} $WebView2ExitCode != 3010
      MessageBox MB_ICONSTOP|MB_OK "L’installation de WebView2 a échoué (code $WebView2ExitCode). RGAA Studio n’a pas été installé."
      Abort
    ${EndIf}
    Call DetectWebView2
    ${If} $R1 == ""
      MessageBox MB_ICONSTOP|MB_OK "WebView2 n’a pas pu être détecté après son installation. RGAA Studio n’a pas été installé."
      Abort
    ${EndIf}
  ${EndIf}
  SetOutPath "$TEMP"
  RMDir /r "$INSTDIR"
  CreateDirectory "$INSTDIR"
  SetOutPath "$INSTDIR"
  File /r "${APP_PAYLOAD_DIR}\*"
  File /oname=WebView2-Runtime-Notice.txt "${APP_WEBVIEW2_NOTICE}"

  CreateDirectory "$SMPROGRAMS\RGAA Studio"
  CreateShortCut "$SMPROGRAMS\RGAA Studio\RGAA Studio.lnk" "$INSTDIR\RgaaStudio.Desktop.exe" "" "$INSTDIR\RgaaStudio.Desktop.exe" 0
  WriteRegStr HKCU "Software\RGAAStudio" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "DisplayName" "RGAA Studio"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "Publisher" "numera11y"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "DisplayIcon" "$INSTDIR\RgaaStudio.Desktop.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio" "NoRepair" 1
  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section /o "Créer un raccourci sur le Bureau" SecDesktopShortcut
  CreateShortCut "$DESKTOP\RGAA Studio.lnk" "$INSTDIR\RgaaStudio.Desktop.exe" "" "$INSTDIR\RgaaStudio.Desktop.exe" 0
SectionEnd

Function un.onInit
  SetShellVarContext current
  FindWindow $R0 "" "RGAA Studio"
  ${If} $R0 != 0
    MessageBox MB_ICONEXCLAMATION|MB_OK "Fermez RGAA Studio avant de le désinstaller."
    Abort
  ${EndIf}
FunctionEnd

Section "Uninstall"
  Delete "$DESKTOP\RGAA Studio.lnk"
  Delete "$SMPROGRAMS\RGAA Studio\RGAA Studio.lnk"
  RMDir "$SMPROGRAMS\RGAA Studio"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RGAAStudio"
  DeleteRegKey HKCU "Software\RGAAStudio"
  RMDir /r "$INSTDIR"
SectionEnd
