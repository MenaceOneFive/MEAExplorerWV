


<h1>소개</h1>

이 리포지토리는 Zenhax커뮤니티의 WarrentyVoider님의 [zeroKilo/MEAExplorerWV](https://github.com/zeroKilo/MEAExplorerWV) 포크버전입니다. "매스이펙트 안드로메다" 한글 패치를 위해 포크 되었습니다.

한국어 지원과 FrostyEditor에서 추출된 파일 지원을 위해 PluginSystem.dll에 해당하는 소스만 수정되었습니다. 

해당 dll에 대한 프로젝트만 빌드하여 사용하고자 하는 프로젝트의 의존성에 추가하시면 됩니다.
사용방법을 모르신다면 이 리포지토리의 PluginTalktableWV안에 있는 파일들을 참고하시면 됩니다.

끝으로 이런 좋은 코드를 공개해주신 zeroKilo(WarrentyVoider)님께 감사의 인사를 남깁니다.

This repository is forked version of [zeroKilo/MEAExplorerWV](https://github.com/zeroKilo/MEAExplorerWV) 
The reason why I forked this repository is to create Localized texttable of ME:A(Mass effect Andromeda) 

I changed a bit of  PluginSystem because Original Source dosen'
t support my language(Korean) and cannot read 'talktable .res' file which came from Frosty Mod Editor because of first 16byte of the file

Build PluginSystem and add dll to your project's dependency.
If you have no idea of how to use this, reading 'PluginTalktableWV' project is recommended

Lastly, I would like to say thank you to zeroKilo(WarrentyVoider) who gave this awesome codes 


<h1>원본

<h2>Introduction</h2>

a tool for modding the game "Mass Effect Andromeda"

this toolset is no longer maintained


<h2> This repository includes</h2>

1.   AnselSDK64: add these dlls to main game folder to enable modding/signature bypassing
1.   FBXWrapper: wrapper dll for fbx sdk

1.   MEAExplorerWV: main tool

1.   PluginSystem: interface dll for creating plugins

1.   PluginTalktableWV: plugin to edit talktables

1.   PluginTexturesWV: plugin to browse, export and import textures

1.   PluginMeshesWV: plugin to browse and export meshes

1.   PluginEbxWV: plugin to browse, edit, export and import ebx (binary xml)

1.   TestPlugin: demo plugin showing how to search all content

1.   TestPlugin2: demo plugin to find and replace data by sha1, also make mod jobs

1.   TestPlugin3: demo plugin to scan for EBX GUIDs

<h2>Support and Useful links</h2>

support and progress here https://zenhax.com/viewtopic.php?f=9&t=3971 

and here https://www.reddit.com/r/meamodding/

wiki and help: http://meamodding.wikia.com/wiki/Mass_Effect_Andromeda_modding_tools_Wiki
</h1>
