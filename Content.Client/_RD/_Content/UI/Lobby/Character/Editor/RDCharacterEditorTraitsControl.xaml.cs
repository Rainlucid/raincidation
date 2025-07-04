﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._RD._Content.UI.Lobby.Character.Editor;

[GenerateTypedNameReferences]
public sealed partial class RDCharacterEditorTraitsControl : BoxContainer
{
    public RDCharacterEditorTraitsControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }
}
