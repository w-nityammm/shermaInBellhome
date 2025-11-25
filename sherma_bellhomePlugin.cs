using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ShermaInBellHome
{
    [BepInPlugin("com.w-nityammm.ShermaInBellHome", "ShermaInBellHome", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private HeroController heroController;
        private int saveFileIdx;
        private GameObject inspect;

        private List<ConfigEntry<bool>> shermaTakenConfigs = new List<ConfigEntry<bool>>();
        private List<ConfigEntry<bool>> shermaInBellHomeConfigs = new List<ConfigEntry<bool>>();

        private bool loadedInBellHome = false;

        private List<string> noNoSceneNames = new List<string> { "Pre_Menu_Loader", "Pre_Menu_Intro", "Menu_Title", "Song_Enclave" };

        private bool shermaSceneLoaded = false;
        private AsyncOperationHandle<SceneInstance> shermaSceneLoadOp;

        private void Awake()
        {
            Log = Logger;
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);

            for (int i = 0; i < 3; i++)
            {
                this.shermaTakenConfigs.Add(base.Config.Bind<bool>(string.Format("Save File {0}", i), "shermaTaken", false, ""));
                this.shermaInBellHomeConfigs.Add(base.Config.Bind<bool>(string.Format("Save File {0}", i), "shermaInBellHome", false, ""));
            }
        }

        private void Update()
        {
            if (this.heroController == null)
            {
                this.heroController = HeroController.instance;
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (this.heroController != null)
            {
                try
                {
                    this.saveFileIdx = this.heroController.playerData.profileID - 1;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Could not get saveFileIdx.");
                }
            }

            bool flag2 = scene.name != "Belltown_Room_Spare" && this.shermaSceneLoaded;
            if (flag2)
            {
                Addressables.UnloadSceneAsync(this.shermaSceneLoadOp.Result, true);
                this.shermaSceneLoaded = false;
            }

            if (this.saveFileIdx >= 0 && this.saveFileIdx < 3)
            {
                bool flag3 = scene.name == "Belltown_Room_Spare" && this.shermaInBellHomeConfigs[this.saveFileIdx].Value;
                if (flag3)
                {
                    base.StartCoroutine(this.LoadSherma());
                }

                bool flag4 = !this.noNoSceneNames.Contains(scene.name) && this.shermaTakenConfigs[this.saveFileIdx].Value && !this.shermaInBellHomeConfigs[this.saveFileIdx].Value;
                if (flag4)
                {
                    this.shermaInBellHomeConfigs[this.saveFileIdx].Value = true;
                    base.Config.Save();
                }
            }

            bool flag5 = scene.name == "Song_Enclave" &&
                         this.heroController != null &&
                         this.heroController.playerData.GetBool("shermaCaretakerConvo1");

            if (flag5)
            {
                if (this.saveFileIdx >= 0 && this.saveFileIdx < 3)
                {
                    bool flag6 = !this.shermaInBellHomeConfigs[this.saveFileIdx].Value;
                    if (flag6)
                    {
                        base.StartCoroutine(this.AddInspect());
                    }
                    else
                    {
                        base.StartCoroutine(this.RemoveSherma());
                    }
                }
            }
        }

        private IEnumerator LoadSherma()
        {
            this.loadedInBellHome = true;

            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync("Scenes/Song_Enclave", LoadSceneMode.Additive, true, 100);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                SceneInstance sceneInstance = handle.Result;
                this.shermaSceneLoadOp = handle;
                Scene shermaScene = sceneInstance.Scene;
                yield return null;
                this.shermaSceneLoaded = true;

                if (shermaScene.IsValid() && shermaScene.isLoaded)
                {
                    foreach (GameObject root in shermaScene.GetRootGameObjects())
                    {
                        Transform shermaTransform = this.FindChild(root.transform, "Sherma Caretaker");

                        if (shermaTransform == null)
                        {
                            Object.Destroy(root.gameObject);
                        }
                        else
                        {
                            shermaTransform.SetParent(null, true);
                            Object.Destroy(root.gameObject);

                            if (this.heroController != null && this.heroController.playerData.GetBool("BelltownFurnishingSpa"))
                            {
                                shermaTransform.position = new Vector3(22.45f, 13.9f, 0.0051f);
                            }
                            else
                            {
                                shermaTransform.position = new Vector3(21f, 7.1f, 0.0051f);
                            }
                        }
                    }
                }
            }
            this.loadedInBellHome = false;
        }

        private Transform FindChild(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform result = this.FindChild(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private IEnumerator RemoveSherma()
        {
            yield return new WaitForSeconds(0.1f);

            string shermaPath = "Black Thread States/Black Thread World/Enclave Act 3/Sherma Caretaker";
            GameObject sherma = GameObject.Find(shermaPath);

            if (sherma != null)
            {
                Object.Destroy(sherma);
                Log.LogInfo("ShermaInBellHome: Removed Sherma from scene.");
            }
            else
            {
                Log.LogWarning($"ShermaInBellHome: Could not find Sherma at path '{shermaPath}' to remove!");
            }
            yield break;
        }

        private IEnumerator AddInspect()
        {
            yield return new WaitForSeconds(0.1f);

            string shermaPath = "Black Thread States/Black Thread World/Enclave Act 3/Sherma Caretaker";
            GameObject sherma = GameObject.Find(shermaPath);

            if (sherma == null)
            {
                Log.LogError($"ShermaInBellHome: Could not find Sherma at path '{shermaPath}'");
                yield break;
            }

            Log.LogInfo("ShermaInBellHome: Found Sherma");

            this.inspect = Object.Instantiate<GameObject>(new GameObject(), sherma.transform.position, Quaternion.identity);

            BoxCollider2D col = this.inspect.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(4f, 4f);

            InteractEvents ie = this.inspect.AddComponent<InteractEvents>();
            ie.InteractLabel = 0; 
            ie.Interacted += delegate
            {
                if (this.heroController == null) return;
                this.heroController.RelinquishControl();
                DialogueYesNoBox.Open(delegate
                {
                    this.heroController.RegainControl();
                    this.InspectDialogue(true);
                }, delegate
                {
                    this.heroController.RegainControl();
                    this.InspectDialogue(false);
                }, true, "Take Sherma to Bellhome", null);
            };
            yield break;
        }

        private void InspectDialogue(bool answer)
        {
            if (answer)
            {
                if (this.saveFileIdx >= 0 && this.saveFileIdx < 3)
                {
                    this.shermaTakenConfigs[this.saveFileIdx].Value = true;
                    base.Config.Save();
                }
                Object.Destroy(this.inspect);
                base.StartCoroutine(this.RemoveSherma());
            }
        }
    }
}