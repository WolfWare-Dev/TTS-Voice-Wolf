﻿using OSCVRCWiz.Services.Integrations;
using OSCVRCWiz.Services.Text;
using System.Text.Json.Nodes;
using System.Windows.Shapes;

namespace OSCVRCWiz.Services.Speech.TextToSpeech {
    public class TTSMessageQueue {
        public static Queue<TTSMessage> queueTTS = new Queue<TTSMessage>();
        public static bool isTTSPlaying = false;
        public struct TTSMessage //use then when setting up presets
        {
            public string text;
            public string TTSMode;
            public string Voice;
            public string Accent;
            public string SpokenLang;
            public string TranslateLang;
            public string Style;
            public int Pitch;
            public int Volume;
            public int Speed;
            public string STTMode;
            public string AzureTranslateText;
            public bool chatboxOverride;
            public bool useChatbox;
            public bool useKAT;
        }
        public static void Enqueue(TTSMessage message)
        {

            queueTTS.Enqueue(message);
            VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
            {
                VoiceWizardWindow.MainFormGlobal.labelQueueSize.Text = queueTTS.Count.ToString();
            });
            // OutputText.outputLog("Enqueued, queue has this many messages:" + queueTTS.Count);
            PlayNext();
        }
        private static void PlayNext()
        {
            if (!isTTSPlaying && queueTTS.Count > 0)
            {
                TTSMessage message = queueTTS.Dequeue();
                VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                {
                    VoiceWizardWindow.MainFormGlobal.labelQueueSize.Text = queueTTS.Count.ToString();
                });
                isTTSPlaying = true;
                Task.Run(() => DoSpeech.MainDoTTS(message));
            }
        }
        public static async Task PlayNextInQueue()
        {

            if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonQueueSystem.Checked == true)
            {
                // OutputText.outputLog("Message finished playing the queue has this many messages:" + queueTTS.Count);


                if (isTTSPlaying == true)
                {

                    Task.Delay(int.Parse(VoiceWizardWindow.MainFormGlobal.textBoxQueueDelayBeforeNext.Text.ToString())).Wait();


                }
                isTTSPlaying = false;
                // Task.Delay(100);
                PlayNext();
            }

        }

        public static void QueueJSONMessage(string message, string STTMode = "Web App")
        {
            OutputText.outputLog(message);
            JsonNode? node = JsonNode.Parse(message);

            if (node is JsonObject jsonObject)
            {
                // Now you can use jsonObject safely
                string text = jsonObject["message"].GetValue<string>();
                QueueMessage(text, STTMode, "[ERROR]", false, true, true, jsonObject);
            } else
            {
                Console.WriteLine("The JSON is not an object.");
            }


        }
        public static void QueueMessage(string text, string STTMode, string AzureTranslate = "[ERROR]", bool chatboxOverride = false, bool useChatbox = true, bool useKAT = true, JsonObject json_message = null)
        {
            try
            {
                if (text == null)
                {
                    OutputText.outputLog("[Message Queue Error: No text found", Color.Red);
                    return;
                }
                text = text.Replace("\n", "");
                string inputText = text;
                string firstString = "";
                string secondString = "";
                int maxLength = 295;
                if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonSmartStringSplit.Checked == true)
                {
                    maxLength = Int32.Parse(VoiceWizardWindow.MainFormGlobal.textBoxSSSCharLimit.Text.ToString());

                    if (inputText.Length > maxLength)
                    {

                        if (char.IsWhiteSpace(inputText[maxLength]))
                        {
                            // Split at the exact 300th character where a space is found
                            firstString = inputText.Substring(0, maxLength);
                            secondString = inputText.Substring(maxLength + 1);
                            text = firstString;
                        } else
                        {
                            int index = maxLength;
                            while (index >= 0 && !char.IsWhiteSpace(inputText[index]))
                            {
                                index--;
                            }

                            firstString = inputText.Substring(0, index);
                            secondString = inputText.Substring(index + 1);
                            text = firstString;
                        }


                    }
                }



                TTSMessageQueue.TTSMessage TTSMessageQueued = new TTSMessageQueue.TTSMessage();
                VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                {
                    TTSMessageQueued.text = text;
                    if (STTMode == "OSCListener-NoTTS")
                    {
                        TTSMessageQueued.TTSMode = "No TTS";
                    } else
                    {
                        TTSMessageQueued.TTSMode = VoiceWizardWindow.MainFormGlobal.comboBoxTTSMode.Text.ToString();
                    }

                    TTSMessageQueued.Voice = GetJsonOrDefault(json_message, "voice", VoiceWizardWindow.MainFormGlobal.comboBoxVoiceSelect.Text.ToString());
                    TTSMessageQueued.Accent = GetJsonOrDefault(json_message, "accent", VoiceWizardWindow.MainFormGlobal.comboBoxAccentSelect.Text.ToString());
                    TTSMessageQueued.Style = GetJsonOrDefault(json_message, "style", VoiceWizardWindow.MainFormGlobal.comboBoxStyleSelect.Text.ToString());
                    TTSMessageQueued.Pitch = GetJsonOrDefault(json_message, "pitch", VoiceWizardWindow.MainFormGlobal.trackBarPitch.Value);
                    TTSMessageQueued.Speed = GetJsonOrDefault(json_message, "speed", VoiceWizardWindow.MainFormGlobal.trackBarSpeed.Value);
                    TTSMessageQueued.Volume = GetJsonOrDefault(json_message, "volume", VoiceWizardWindow.MainFormGlobal.trackBarVolume.Value);

                    TTSMessageQueued.SpokenLang = VoiceWizardWindow.MainFormGlobal.comboBoxSpokenLanguage.Text.ToString();
                    TTSMessageQueued.TranslateLang = VoiceWizardWindow.MainFormGlobal.comboBoxTranslationLanguage.Text.ToString();
                    TTSMessageQueued.STTMode = STTMode;
                    TTSMessageQueued.AzureTranslateText = AzureTranslate;
                    TTSMessageQueued.chatboxOverride = chatboxOverride;
                    if (chatboxOverride == true)
                    {
                        TTSMessageQueued.useChatbox = useChatbox;
                        TTSMessageQueued.useKAT = useKAT;
                    }
                });
                if (STTMode == "Text")
                {
                    if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonQueueSystem.Checked == true && VoiceWizardWindow.MainFormGlobal.rjToggleButtonQueueTypedText.Checked == true)
                    {
                        TTSMessageQueue.Enqueue(TTSMessageQueued);
                    } else
                    {
                        Task.Run(() => DoSpeech.MainDoTTS(TTSMessageQueued));
                    }

                } else
                {
                    if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonQueueSystem.Checked == true)
                    {

                        TTSMessageQueue.Enqueue(TTSMessageQueued);
                    } else
                    {
                        Task.Run(() => DoSpeech.MainDoTTS(TTSMessageQueued));
                    }
                }

                if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonSmartStringSplit.Checked == true)
                {
                    if (inputText.Length > maxLength)
                    {
                        QueueMessage(secondString, STTMode, AzureTranslate);
                    }
                }

            }
            catch (Exception ex)
            {
                OutputText.outputLog("[TTS Queue Message Error: " + ex.Message + "]", Color.Red);
                {

                }
            }

        }

        private static int GetJsonOrDefault(JsonObject json, string key, int defaultValue)
        {
            return json != null && json[key] != null ? json[key].GetValue<int>() : defaultValue;
        }

        private static string GetJsonOrDefault(JsonObject json, string key, string defaultValue)
        {
            return json != null && json[key] != null ? json[key].GetValue<string>() : defaultValue;
        }




    }
}
