using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Shared PC overlay helpers. Coordinates are confined to the window safe area.
    public static class PolygonDesktopHUD
    {
        public static Rect SafeArea
        {
            get
            {
                var r=Screen.safeArea;
                return new Rect(r.x,Screen.height-r.yMax,r.width,r.height);
            }
        }
        public static float Scale(Rect safe)=>Mathf.Clamp(Mathf.Min(safe.width/960f,safe.height/540f),.55f,1.25f);
        public static Rect RaceRect(Rect safe,float s)=>new Rect(safe.x+12*s,safe.y+12*s,Mathf.Min(232*s,safe.width-24*s),88*s);
        public static Rect HelpRect(Rect safe,float s,bool paused)
        {
            float w=Mathf.Min(270*s,safe.width-24*s),h=(paused?164:100)*s;
            float x=safe.xMax-12*s-w,y=safe.y+12*s;
            if(safe.width<540*s)y+=98*s;
            return new Rect(x,y,w,Mathf.Min(h,Mathf.Max(0,safe.yMax-y-12*s)));
        }
        public static void Panel(Rect rect,float alpha=1)
        {
            var old=GUI.color;GUI.color=new Color(.025f,.055f,.08f,.77f*alpha);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
        }
        static readonly Dictionary<int,GUIStyle> labels=new Dictionary<int,GUIStyle>();
        static GUIStyle labelBasis;
        public static void Label(Rect rect,string text,int size,TextAnchor anchor,float alpha=1)
        {
            if(labelBasis!=GUI.skin.label){labels.Clear();labelBasis=GUI.skin.label;}
            int key=size*16+(int)anchor;
            if(!labels.TryGetValue(key,out var style))
            {
                style=new GUIStyle(labelBasis){fontSize=size,alignment=anchor,clipping=TextClipping.Clip};
                style.normal.textColor=new Color(.90f,.97f,.96f,1);labels.Add(key,style);
            }
            var old=GUI.color;GUI.color=new Color(1,1,1,alpha);GUI.Label(rect,text,style);GUI.color=old;
        }
        public static void Race(Rect rect,float s,string heading,string lap,string current,string best)
        {
            Panel(rect);float pad=10*s;int small=Mathf.Max(10,Mathf.RoundToInt(13*s));
            Label(new Rect(rect.x+pad,rect.y+5*s,rect.width-2*pad,22*s),heading,Mathf.Max(12,Mathf.RoundToInt(16*s)),TextAnchor.MiddleLeft);
            Label(new Rect(rect.x+pad,rect.y+29*s,rect.width-2*pad,17*s),lap,small,TextAnchor.MiddleLeft);
            Label(new Rect(rect.x+pad,rect.y+47*s,rect.width-2*pad,17*s),"CURRENT  "+current,small,TextAnchor.MiddleLeft);
            Label(new Rect(rect.x+pad,rect.y+65*s,rect.width-2*pad,17*s),"BEST   "+best,small,TextAnchor.MiddleLeft);
        }
        public static void Debug(Rect safe,Rect race,Rect help,float s,float fps,PolygonDriver player)
        {
            float y=safe.width<540*s?help.yMax+8*s:race.yMax+8*s;
            var r=new Rect(race.x,y,race.width,Mathf.Min(66*s,Mathf.Max(0,safe.yMax-y-12*s)));
            if(r.height<=0)return;Panel(r);
            string value=$"DEBUG  ?  F3 TO HIDE\n{fps:0} FPS   {player.SpeedMph:0} MPH\nGEAR {player.Drivetrain.Gear}   {player.Drivetrain.Rpm:0} RPM";
            Label(new Rect(r.x+10*s,r.y+5*s,r.width-20*s,r.height-10*s),value,Mathf.Max(10,Mathf.RoundToInt(12*s)),TextAnchor.UpperLeft);
        }
        public static string ControlText(bool gamepad,bool includeMenu)
        {
            if(gamepad)return "RT: Accelerate   LT: Brake\nLeft stick: Steer   D-pad up: View\nA: Restart   Start: Pause"+(includeMenu?"\nY: Race menu":"");
            return "W / Up: Accelerate\nS / Down / Space: Brake\nA / D / Arrows: Steer\nR: Restart  P: Pause  V: View"+(includeMenu?"  M: Menu":"");
        }
        public static void Help(Rect rect,float s,bool gamepad,bool paused,bool includeMenu,float alpha=1)
        {
            if(rect.height<=0)return;Panel(rect,alpha);
            float pad=10*s;int body=Mathf.Max(10,Mathf.RoundToInt(13*s));
            Label(new Rect(rect.x+pad,rect.y+5*s,rect.width-2*pad,20*s),paused?"PAUSED  ?  CONTROLS":"CONTROLS",Mathf.Max(11,Mathf.RoundToInt(14*s)),TextAnchor.MiddleLeft,alpha);
            Label(new Rect(rect.x+pad,rect.y+26*s,rect.width-2*pad,Mathf.Min(rect.height-28*s,68*s)),ControlText(gamepad,includeMenu),body,TextAnchor.UpperLeft,alpha);
            if(paused&&rect.height>=155*s)
                Label(new Rect(rect.x+pad,rect.y+137*s,rect.width-2*pad,18*s),"F3: DEBUG",Mathf.Max(10,Mathf.RoundToInt(11*s)),TextAnchor.MiddleLeft,alpha);
        }
    }
}
