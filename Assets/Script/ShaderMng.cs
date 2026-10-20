// Modified for NUKE HOUR iOS/resource-free adaptation, 2026. Original notices retained.
﻿using UnityEngine;
using System.Collections;

public class ShaderMng  {
    public static Shader Find(string shader)
    {
        int nindex = shader.LastIndexOf('/');
        string sub = shader.Substring(nindex + 1); 
        Shader sh = global::Meteor.Launcher.ContentResources.Load<Shader>(sub);
        return sh;
    }
}
