﻿using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    //enum MessageFont : byte
    //{
    //    INVALID = 0xFF,
    //    Bold = 0,
    //    Shadow = 1,
    //    BoldShadow = 2,
    //    Normal = 3,
    //    Gothic = 4,
    //    Italic = 5,
    //    SmallDark = 6,
    //    Colorful = 7,
    //    Rune = 8,
    //    SmallLight = 9
    //}

    internal enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }


    internal static class MessageManager
    {
        public static PromptData PromptData { get; set; }

        public static event EventHandler<MessageEventArgs> MessageReceived;

        public static event EventHandler<MessageEventArgs> LocalizedMessageReceived;


        public static void HandleMessage
        (
            Entity parent,
            string text,
            string name,
            ushort hue,
            MessageType type,
            byte font,
            TextType textType,
            bool unicode = false,
            string lang = null
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile != null && currentProfile.OverrideAllFonts)
            {
                font = currentProfile.ChatFont;
                unicode = currentProfile.OverrideAllFontsIsUnicode;
            }

            switch (type)
            {
                case MessageType.Command:
                case MessageType.Encoded:
                case MessageType.System:
                case MessageType.Party:
                case MessageType.Guild:
                case MessageType.Alliance: break;


                case MessageType.Spell:

                {
                    //server hue color per default
                    if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue
                        (text, out SpellDefinition spell))
                    {
                        if (currentProfile != null && currentProfile.EnabledSpellFormat &&
                            !string.IsNullOrWhiteSpace(currentProfile.SpellDisplayFormat))
                        {
                            StringBuilder sb = new StringBuilder(currentProfile.SpellDisplayFormat);
                            sb.Replace("{power}", spell.PowerWords);
                            sb.Replace("{spell}", spell.Name);

                            text = sb.ToString().Trim();
                        }

                        //server hue color per default if not enabled
                        if (currentProfile != null && currentProfile.EnabledSpellHue)
                        {
                            if (spell.TargetType == TargetType.Beneficial)
                            {
                                hue = currentProfile.BeneficHue;
                            }
                            else if (spell.TargetType == TargetType.Harmful)
                            {
                                hue = currentProfile.HarmfulHue;
                            }
                            else
                            {
                                hue = currentProfile.NeutralHue;
                            }
                        }
                    }

                    goto case MessageType.Label;
                }

                default:
                case MessageType.Focus:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Regular:
                case MessageType.Label:
                case MessageType.Limit3Spell:

                    if (parent == null)
                    {
                        break;
                    }

                    TextObject msg = CreateMessage(text, hue, font, unicode, type, textType);
                    msg.Owner = parent;

                    if (parent is Item it && !it.OnGround)
                    {
                        msg.X = DelayedObjectClickManager.X;
                        msg.Y = DelayedObjectClickManager.Y;
                        msg.IsTextGump = true;
                        bool found = false;

                        for (LinkedListNode<Control> gump = UIManager.Gumps.Last; gump != null; gump = gump.Previous)
                        {
                            Control g = gump.Value;

                            if (!g.IsDisposed)
                            {
                                switch (g)
                                {
                                    case PaperDollGump paperDoll when g.LocalSerial == it.Container:
                                        paperDoll.AddText(msg);
                                        found = true;

                                        break;

                                    case ContainerGump container when g.LocalSerial == it.Container:
                                        container.AddText(msg);
                                        found = true;

                                        break;

                                    case TradingGump trade when trade.ID1 == it.Container || trade.ID2 == it.Container:
                                        trade.AddText(msg);
                                        found = true;

                                        break;
                                }
                            }

                            if (found)
                            {
                                break;
                            }
                        }
                    }

                    parent.AddMessage(msg);

                    break;


                //default:
                //    if (parent == null)
                //        break;

                //    parent.AddMessage(type, text, font, hue, unicode);

                //    break;
            }

            MessageReceived.Raise
                (new MessageEventArgs(parent, text, name, hue, type, font, textType, unicode, lang), parent);
        }

        public static void OnLocalizedMessage(Entity entity, MessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }

        public static TextObject CreateMessage
        (
            string msg,
            ushort hue,
            byte font,
            bool isunicode,
            MessageType type,
            TextType textType
        )
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.OverrideAllFonts)
            {
                font = ProfileManager.CurrentProfile.ChatFont;
                isunicode = ProfileManager.CurrentProfile.OverrideAllFontsIsUnicode;
            }

            int width = isunicode
                ? FontsLoader.Instance.GetWidthUnicode(font, msg)
                : FontsLoader.Instance.GetWidthASCII(font, msg);

            if (width > 200)
            {
                width = isunicode
                    ? FontsLoader.Instance.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder)
                    : FontsLoader.Instance.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder);
            }
            else
            {
                width = 0;
            }

            TextObject textObject = TextObject.Create();
            textObject.Alpha = 0xFF;
            textObject.Type = type;
            textObject.Hue = hue;

            if (!isunicode && textType == TextType.OBJECT)
            {
                hue = 0;
            }

            textObject.RenderedText = RenderedText.Create
            (
                msg, hue, font, isunicode, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT, width, 30, false, false,
                textType == TextType.OBJECT
            );

            textObject.Time = CalculateTimeToLive(textObject.RenderedText);
            textObject.RenderedText.Hue = textObject.Hue;

            return textObject;
        }

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            long timeToLive;

            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile.ScaleSpeechDelay)
            {
                int delay = currentProfile.SpeechDelay;

                if (delay < 10)
                {
                    delay = 10;
                }

                timeToLive = (long) (4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * currentProfile.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Time.Ticks;

            return timeToLive;
        }
    }
}