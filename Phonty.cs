using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using HarmonyLib;

namespace PhontyPlus {
    public class Phonty : NPC, IClickable<int> {
        public static Sprite idle;
        public static Sprite deafIcon;
        public static List<SoundObject> records = new List<SoundObject>();
        public static AssetManager audios = new AssetManager();
        public static List<Sprite> emergeFrames = new List<Sprite>();
        public static List<Sprite> chaseFrames = new List<Sprite>();
        public CustomSpriteRendererAnimator animator;
        public AudioManager audMan;
        public TextMeshPro counter;
        public GameObject totalDisplay;
        public MapIcon mapIconPre;
        public NoLateIcon mapIcon;
        public bool angry = false;
        private bool deafPlayer = false;
        private int currentDisplayTime = -1;

        public static void LoadAssets() {
            var PIXELS_PER_UNIT = 26f;
            idle = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Idle.png"), PIXELS_PER_UNIT);

            try {
                deafIcon = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Player_Deaf_Icon.png"), 50f);
            }
            catch {
                deafIcon = idle;
            }

            audios.Add("angryIntro", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "PhontyIntro.ogg"), "Phonty_Vfx_Intro", SoundType.Voice, Color.yellow));
            audios.Add("angry", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "PhontyAngry.ogg"), "Phonty_Vfx_Angry", SoundType.Voice, Color.yellow));
            audios.Add("shockwave", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "PhontyShot.ogg"), "Phonty_Sfx_Shot", SoundType.Effect, Color.yellow));

            SoundObject windup = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "ClockWind.wav"), "Vfx_Phonty_Wind", SoundType.Effect, Color.white);
            windup.subtitle = false;
            audios.Add("windup", windup);

            emergeFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 4, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Emerge0.png")));
            emergeFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 4, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Emerge1.png")));

            Sprite[] emergeSheet2 = AssetLoader.SpritesFromSpritesheet(4, 2, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Emerge2.png"));
            for (int i = 7; i > 5; i--) {
                Object.DestroyImmediate(emergeSheet2[i]);
            }
            for (int i = 0; i < 6; i++) {
                emergeFrames.Add(emergeSheet2[i]);
            }

            chaseFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 4, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Chase0.png")));
            chaseFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 1, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Chase1.png")));

            var recordsFolder = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(Mod.Instance), "Audio", "Records"));
            foreach (var path in recordsFolder)
            {
                records.Add(ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(path), "Phonty_Vfx_Record", SoundType.Voice, Color.yellow));
            }
        }

        public override void Initialize() {
            base.Initialize();

            animator.AddAnimation("Idle", new SpriteAnimation(new[] { idle }, 1f));
            animator.AddAnimation("Chase", new SpriteAnimation(chaseFrames.ToArray(), 0.5f));
            animator.AddAnimation("ChaseStatic", new SpriteAnimation(new[] { emergeFrames.Last() }, 1f));
            animator.AddAnimation("Emerge", new SpriteAnimation(emergeFrames.ToArray(), 1f));
            animator.SetDefaultAnimation("Idle", 1f);

            behaviorStateMachine.ChangeState(new Phonty_PlayingMusic(this));
            navigator.SetSpeed(0f);
            navigator.maxSpeed = 0f;
            navigator.Entity.SetHeight(7f);
            gameObject.layer = LayerMask.NameToLayer("ClickableEntities");

            StartCoroutine(DelayedInitialize());
        }

        private IEnumerator DelayedInitialize() {
            yield return null;

            if (!Mod.gameAssetsLoaded) {
                try {
                    var mathMachinePrefab = Resources.FindObjectsOfTypeAll<MathMachine>().FirstOrDefault(x => x.name == "MathMachine");
                    if (mathMachinePrefab != null) {
                        var totalTmpField = Traverse.Create(mathMachinePrefab).Field<TMP_Text>("totalTmp").Value;
                        Mod.assetManager.Add("TotalBase", totalTmpField.transform.parent.gameObject);
                    }

                    Mod.assetManager.Add("MapIcon", Resources.FindObjectsOfTypeAll<NoLateIcon>().First(x => x.name == "MapIcon" && x.GetInstanceID() >= 0));

                    var silenceRoom = Resources.FindObjectsOfTypeAll<SilenceRoomFunction>().First(x => x.name == "LibraryRoomFunction" && x.GetInstanceID() >= 0);
                    var mixer = Traverse.Create(silenceRoom).Field<AudioMixer>("mixer").Value;
                    Mod.assetManager.Add("Mixer", mixer);
                    Mod.GlobalMixer = mixer;

                    Mod.gameAssetsLoaded = true;
                }
                catch (System.Exception e) {
                    Debug.LogError($"PhontyPlus: Critical error during lazy asset loading! {e}");
                }
            }

            var totalBasePrefab = Mod.assetManager.Get<GameObject>("TotalBase");
            if (totalBasePrefab != null) {
                GameObject instantiatedTotalBase = Object.Instantiate(totalBasePrefab, transform);
                instantiatedTotalBase.transform.localPosition = new Vector3(0, 3, 0);
                instantiatedTotalBase.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                instantiatedTotalBase.SetActive(true);
                totalDisplay = instantiatedTotalBase.transform.GetChild(0).gameObject;
                totalDisplay.SetActive(true);
                counter = totalDisplay.GetComponent<TextMeshPro>();
            }

            mapIconPre = Mod.assetManager.Get<NoLateIcon>("MapIcon");
            if (mapIconPre != null) {
                mapIcon = (NoLateIcon)ec.map.AddIcon(mapIconPre, gameObject.transform, Color.white);
                mapIcon.spriteRenderer.sprite = idle;
                Object.DestroyImmediate(mapIcon.GetComponent<Animator>());
                mapIcon.gameObject.SetActive(true);
            }
        }

        protected override void VirtualUpdate() {
            if (deafPlayer && AudioListener.volume > 0.01f) {
                AudioListener.volume = 0.01f;
            }
        }

        private IEnumerator SubtitlesAlpha(float alphaValue, float duration) {
            var SubMan = SubtitleManager.Instance.gameObject.GetComponent<CanvasGroup>();

            float elapsedTime = 0f;
            while (elapsedTime < duration) {
                elapsedTime += Time.deltaTime;
                SubMan.alpha = Mathf.Lerp(SubMan.alpha, alphaValue, elapsedTime / duration);
                yield return null;
            }

            SubMan.alpha = alphaValue;
            yield break;
        }

        public void Clicked(int player) {
            if (!angry) ResetTimer();
        }

        public void ResetTimer() {
            behaviorStateMachine.ChangeState(new Phonty_PlayingMusic(this, true));
        }

        public void UpdateCounter(int count) {
            if (counter != null && mapIcon != null && count != currentDisplayTime) {
                currentDisplayTime = Mathf.Max(count, 0);
                mapIcon.timeText.text = $"{Mathf.Floor(currentDisplayTime / 60):0}:{currentDisplayTime % 60:00}";
                mapIcon.UpdatePosition(ec.map);
                counter.SetText(string.Join("", count.ToString().Select(ch => "<sprite=" + ch + ">")));
            }
        }

        public void EndGame(Transform player) {
            var core = CoreGameManager.Instance;

            if (PhontyMenu.nonLethalConfig.Value || CoreGameManager.Instance.currentMode == Mode.Free) {
                core.audMan.PlaySingle(audios.Get<SoundObject>("shockwave"));
                angry = false;
                totalDisplay.SetActive(true);
                mapIcon.gameObject.SetActive(true);
                StartCoroutine(DeafenPlayer());
                ResetTimer();
                return;
            }

            Time.timeScale = 0f;
            MusicManager.Instance.StopMidi();
            core.disablePause = true;
            core.GetCamera(0).UpdateTargets(transform, 0);
            core.GetCamera(0).offestPos = (player.position - transform.position).normalized * 2f + Vector3.up;
            core.GetCamera(0).SetControllable(false);
            core.GetCamera(0).matchTargetRotation = false;
            core.audMan.volumeModifier = 0.6f;
            core.audMan.PlaySingle(audios.Get<SoundObject>("shockwave"));

            var endSequenceEnumerator = (IEnumerator)Traverse.Create(core).Method("EndSequence").GetValue();
            core.StartCoroutine(endSequenceEnumerator);

            InputManager.Instance.Rumble(1f, 2f);
        }

        private IEnumerator DeafenPlayer() {
            yield return new WaitForSeconds(0.5f);

            float duration = PhontyMenu.deafTimeConfig.Value;
            float timer = duration;
            HudGauge gauge = null;

            var hud = Singleton<CoreGameManager>.Instance.GetHud(0);
            if (hud != null) {
                var gaugeManager = hud.GetComponentInChildren<HudGaugeManager>();
                if (gaugeManager != null) {
                    gauge = gaugeManager.ActivateNewGauge(deafIcon, duration);
                }
            }

            deafPlayer = true;
            StartCoroutine(SubtitlesAlpha(0.01f, 2f));
            AudioListener.volume = 0.01f;
            if (Mod.GlobalMixer != null)
                Mod.GlobalMixer.SetFloat("EchoWetMix", 1f);

            while (timer > 0f) {
                timer -= Time.deltaTime;
                if (gauge != null) {
                    gauge.SetValue(duration, timer);
                }
                yield return null;
            }

            deafPlayer = false;
            AudioListener.volume = 1f;
            if (Mod.GlobalMixer != null)
                Mod.GlobalMixer.SetFloat("EchoWetMix", 0f);

            StartCoroutine(SubtitlesAlpha(1f, 2f));

            if (gauge != null) {
                gauge.Deactivate();
            }
        }

        public bool ClickableHidden() => angry;
        public bool ClickableRequiresNormalHeight() => false;
        public void ClickableSighted(int player) { }
        public void ClickableUnsighted(int player) { }
    }

    public class Phonty_StateBase : NpcState {
        protected Phonty phonty;
        public Phonty_StateBase(Phonty phonty) : base(phonty) {
            this.phonty = phonty;
        }
    }

    public class Phonty_PlayingMusic : Phonty_StateBase {
        private bool interacted;
        protected float timeLeft;

        public Phonty_PlayingMusic(Phonty phonty, bool playerInteraction = false) : base(phonty) {
            interacted = playerInteraction;
            timeLeft = PhontyMenu.timeLeftUntilMad.Value;
        }

        public override void Enter() {
            base.Enter();
            base.ChangeNavigationState(new NavigationState_DoNothing(phonty, 63));
            phonty.audMan.FlushQueue(true);
            if (interacted)
                phonty.audMan.PlaySingle(Phonty.audios.Get<SoundObject>("windup"));

            phonty.audMan.QueueRandomAudio(Phonty.records.ToArray());
            phonty.audMan.SetLoop(true);

            phonty.animator.SetDefaultAnimation("Idle", 1f);
            phonty.animator.Play("Idle", 1f);
        }

        public override void Update() {
            base.Update();
            if (timeLeft <= 0)
                phonty.behaviorStateMachine.ChangeState(new Phonty_Chase(phonty));
            else
                timeLeft -= Time.deltaTime * phonty.ec.NpcTimeScale;

            phonty.UpdateCounter((int)timeLeft);
        }
    }

    public class Phonty_Chase : Phonty_StateBase {
        protected NavigationState_TargetPlayer targetState;
        protected PlayerManager player;

        public Phonty_Chase(Phonty phonty) : base(phonty) {
            player = phonty.ec.Players[0];
            targetState = new NavigationState_TargetPlayer(phonty, 64, player.transform.position);
        }

        public override void Enter() {
            base.Enter();
            targetState = new NavigationState_TargetPlayer(phonty, 64, player.transform.position);
            base.ChangeNavigationState(targetState);
            phonty.angry = true;

            if (phonty.totalDisplay != null) phonty.totalDisplay.SetActive(false);
            if (phonty.mapIcon != null) phonty.mapIcon.gameObject.SetActive(false);

            phonty.audMan.FlushQueue(true);
            phonty.audMan.QueueAudio(Phonty.audios.Get<SoundObject>("angryIntro"), true);

            phonty.animator.Play("Emerge", 1f);
            phonty.animator.SetDefaultAnimation("ChaseStatic", 1f);
            phonty.StartCoroutine(Emerge());
        }

        public override void OnStateTriggerEnter(Collider other, bool validCollision) {
            base.OnStateTriggerEnter(other, validCollision);
            if (other.CompareTag("Player") && other.GetComponent<PlayerManager>() == player)
                phonty.EndGame(other.transform);
        }

        public override void DestinationEmpty() {
            base.DestinationEmpty();
            base.ChangeNavigationState(new NavigationState_WanderRandom(phonty, 32));
        }

        public override void PlayerInSight(PlayerManager player) {
            base.PlayerInSight(player);
            if (this.player == player) {
                base.ChangeNavigationState(targetState);
                targetState.UpdatePosition(player.transform.position);
            }
        }

        private IEnumerator Emerge() {
            while (phonty.audMan.QueuedAudioIsPlaying)
                yield return null;

            if (!(phonty.behaviorStateMachine.currentState is Phonty_Chase))
                yield break;

            phonty.audMan.QueueAudio(Phonty.audios.Get<SoundObject>("angry"), true);
            phonty.audMan.SetLoop(true);

            phonty.animator.SetDefaultAnimation("Chase", 1f);
            phonty.animator.Play("Chase", 1f, true);

            phonty.Navigator.SetSpeed(4f);
            phonty.Navigator.maxSpeed = PhontyMenu.chaseSpeedConfig.Value;
        }
    }

    public class Phonty_Dead : Phonty_StateBase {
        public Phonty_Dead(Phonty phonty) : base(phonty) { }

        public override void Enter() {
            base.Enter();
            base.ChangeNavigationState(new NavigationState_Disabled(phonty));
            phonty.angry = true;
            if (phonty.totalDisplay != null) phonty.totalDisplay.SetActive(false);
            if (phonty.mapIcon != null) phonty.mapIcon.gameObject.SetActive(false);

            phonty.animator.Play("Idle", 1f);
            phonty.animator.SetDefaultAnimation("Idle", 1f);
        }
    }
}