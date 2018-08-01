using InfinityScript;
using System;
using System.Collections.Generic;
namespace Predator
{
    public class Missile : BaseScript
    {
        private int missileRemoteLaunchVert = 14000;
        private int missileRemoteLaunchHorz = 30000;
        private int missileRemoteLaunchTargetDist = 1500;
        private List<Entity> RidingPred = new List<Entity>();
        private bool GameEnded;
        public Missile()
        {
            base.PlayerConnected += delegate(Entity entity)
            {
                entity.Call("notifyonplayercommand", new Parameter[]
				{
					"3",
					"+actionslot 3"
				});
                entity.OnNotify("3", delegate(Entity ent)
                {
                    this.giveKillstreakWeapon(ent, "predator_missile");
                });
                entity.SetField("laptopWait", string.Empty);
                entity.OnNotify("weapon_switch_started", delegate(Entity ent, Parameter weapon)
                {
                    if (ent.GetField<string>("laptopWait") == "get")
                    {
                        entity.SetField("laptopWait", "weapon_switch_started");
                    }
                });
                entity.OnNotify("weapon_change", delegate(Entity ent, Parameter newWeap)
                {
                    if (this.mayDropWeapon((string)newWeap))
                    {
                        ent.SetField("lastDroppableWeapon", (string)newWeap);
                    }
                    this.KillstreakUseWaiter(ent, (string)newWeap);
                });
                entity.OnNotify("joined_team", delegate(Entity ent)
                {
                    if (!this.RidingPred.Contains(ent))
                    {
                        return;
                    }
                    if (ent.GetField<string>("sessionteam") != "spectator")
                    {
                        this.Player_ClearUp(ent);
                    }
                    this.ClearUsingRemote(ent);
                });
                entity.OnNotify("joined_spectators", delegate(Entity ent)
                {
                    if (!this.RidingPred.Contains(ent))
                    {
                        return;
                    }
                    if (ent.GetField<string>("sessionteam") != "spectator")
                    {
                        this.Player_ClearUp(ent);
                    }
                    this.ClearUsingRemote(ent);
                });
                base.OnNotify("game_ended", delegate(Parameter level)
                {
                    this.GameEnded = true;
                    foreach (Entity current in this.RidingPred)
                    {
                        this.Player_ClearUp(current);
                    }
                    this.RidingPred.Clear();
                });
            };
        }
        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (player.GetField<string>("laptopWait") == "get")
            {
                player.SetField("laptopWait", "death");
            }
        }
        private static void SetUsingRemote(Entity ent, string remote = "")
        {
            ent.Call("DisableOffhandWeapons", new Parameter[0]);
            ent.Notify("using_remote", new Parameter[0]);
        }
        private int getKillstreakIndex(string streakName)
        {
            return base.Call<int>("tableLookupRowNum", new Parameter[]
			{
				"mp/killstreakTable.csv",
				1,
				streakName
			}) - 1;
        }
        private string getKillstreakWeapon(string streakName)
        {
            string text = string.Empty;
            text = base.Call<string>("tableLookup", new Parameter[]
			{
				"mp/killstreakTable.csv",
				1,
				streakName,
				12
			});
            Log.Write(LogLevel.Info, "Killstreak weapon: " + text);
            return text;
        }
        private void giveKillstreakWeapon(Entity ent, string streakName)
        {
            string killstreakWeapon = this.getKillstreakWeapon(streakName);
            if (string.IsNullOrEmpty(killstreakWeapon))
            {
                return;
            }
            ent.SetField("customStreak", streakName);
            ent.Call("giveWeapon", new Parameter[]
			{
				killstreakWeapon,
				0,
				false
			});
            ent.Call("setActionSlot", new Parameter[]
			{
				4,
				"weapon",
				killstreakWeapon
			});
            ent.Call("SetPlayerData", new Parameter[]
			{
				"killstreaksState",
				"hasStreak",
				0,
				true
			});
            ent.Call("SetPlayerData", new Parameter[]
			{
				"killstreaksState",
				"icons",
				0,
				this.getKillstreakIndex("predator_missile")
			});
        }
        private void KillstreakUseWaiter(Entity ent, string weapon)
        {
            if (weapon == "killstreak_predator_missile_mp")
            {
                this.tryUsePredator(ent);
                ent.Call("playLocalSound", new Parameter[]
				{
					"weap_c4detpack_trigger_plr"
				});
                return;
            }
            Log.Write(LogLevel.Info, "KillstreakUseWaiter: " + weapon);
        }
        private string GetThermalVision()
        {
            string a = base.Call<string>("getMapCustom", new Parameter[]
			{
				"thermal"
			});
            if (a == "invert")
            {
                return "thermal_snowlevel_mp";
            }
            return "thermal_mp";
        }
        private void Player_ClearUp(Entity player)
        {
            player.Call("ThermalVisionFOFOverlayOff", new Parameter[0]);
            player.Call("ControlsUnlink", new Parameter[0]);
            player.Call("CameraUnlink", new Parameter[0]);
            if (base.Call<int>("getdvarint", new Parameter[]
			{
				"camera_thirdPerson"
			}) == 1)
            {
                Missile.setThirdPersonDOF(player, true);
            }
        }
        private static void setThirdPersonDOF(Entity ent, bool Enabled)
        {
            if (Enabled)
            {
                ent.Call("setDepthOfField", new Parameter[]
				{
					0f,
					110f,
					512f,
					4096f,
					6f,
					1.8f
				});
                return;
            }
            ent.Call("setDepthOfField", new Parameter[]
			{
				0f,
				0f,
				512f,
				512f,
				4f,
				0f
			});
        }
        private void ClearUsingRemote(Entity ent)
        {
            ent.Call("enableOffhandWeapons", new Parameter[0]);
            string currentWeapon = ent.CurrentWeapon;
            if (currentWeapon == "none" || this.isKillstreakWeapon(currentWeapon))
            {
                ent.TakeWeapon(currentWeapon);
                ent.Call("SwitchToWeapon", new Parameter[]
				{
					ent.GetField<string>("lastDroppableWeapon")
				});
            }
            ent.Call("freezeControls", new Parameter[]
			{
				false
			});
            ent.Notify("stopped_using_remote", new Parameter[0]);
        }
        private bool mayDropWeapon(string weapon)
        {
            if (weapon == "none")
            {
                return false;
            }
            if (weapon.Contains("ac130"))
            {
                return false;
            }
            string a = base.Call<string>("WeaponInventoryType", new Parameter[]
			{
				weapon
			});
            return !(a != "primary");
        }
        public static bool isAirdropMarker(string weaponName)
        {
            return weaponName != null && (weaponName == "airdrop_marker_mp" || weaponName == "airdrop_mega_marker_mp" || weaponName == "airdrop_sentry_marker_mp" || weaponName == "airdrop_juggernaut_mp" || weaponName == "airdrop_juggernaut_def_mp");
        }
        public static bool isAssaultKillstreak(string refString)
        {
            switch (refString)
            {
                case "uav":
                case "airdrop_assault":
                case "predator_missile":
                case "ims":
                case "airdrop_sentry_minigun":
                case "precision_airstrike":
                case "helicopter":
                case "littlebird_flock":
                case "littlebird_support":
                case "remote_mortar":
                case "airdrop_remote_tank":
                case "helicopter_flares":
                case "ac130":
                case "airdrop_juggernaut":
                case "osprey_gunner":
                    return true;
            }
            return false;
        }
        public static bool isSupportKillstreak(string refString)
        {
            switch (refString)
            {
                case "uav_support":
                case "counter_uav":
                case "deployable_vest":
                case "airdrop_trap":
                case "sam_turret":
                case "remote_uav":
                case "triple_uav":
                case "remote_mg_turret":
                case "stealth_airstrike":
                case "emp":
                case "airdrop_juggernaut_recon":
                case "escort_airdrop":
                    return true;
            }
            return false;
        }
        public static bool isSpecialistKillstreak(string refString)
        {
            switch (refString)
            {
                case "specialty_longersprint_ks":
                case "specialty_fastreload_ks":
                case "specialty_scavenger_ks":
                case "specialty_blindeye_ks":
                case "specialty_paint_ks":
                case "specialty_hardline_ks":
                case "specialty_coldblooded_ks":
                case "specialty_quickdraw_ks":
                case "specialty_assists_ks":
                case "_specialty_blastshield_ks":
                case "specialty_detectexplosive_ks":
                case "specialty_autospot_ks":
                case "specialty_bulletaccuracy_ks":
                case "specialty_quieter_ks":
                case "specialty_stalker_ks":
                case "all_perks_bonus":
                    return true;
            }
            return false;
        }
        private bool isKillstreakWeapon(string wep)
        {
            if (string.IsNullOrEmpty(wep))
            {
                return false;
            }
            wep = wep.ToLower();
            if (wep == "none")
            {
                return false;
            }
            string[] array = wep.Split(new char[]
			{
				'_'
			});
            bool flag = false;
            if (wep != "destructible_car" && wep != "barrel_mp")
            {
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    string a = array2[i];
                    if (a == "mp")
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    wep += "_mp";
                }
            }
            return !wep.Contains("destructible") && (wep.Contains("killstreak") || Missile.isAirdropMarker(wep) || (wep != "destructible_car" && wep != "barrel_mp" && !string.IsNullOrEmpty(base.Call<string>("weaponInventoryType", new Parameter[]
			{
				wep
			})) && base.Call<string>("weaponInventoryType", new Parameter[]
			{
				wep
			}) == "exclusive") || wep.Contains("remote"));
        }
        private void initRideKillstreak(Entity ent, string streakName = "")
		{
			Missile.<>c__DisplayClassb <>c__DisplayClassb = new Missile.<>c__DisplayClassb();
			<>c__DisplayClassb.streakName = streakName;
			<>c__DisplayClassb.<>4__this = this;
			if (!string.IsNullOrEmpty(<>c__DisplayClassb.streakName) && (<>c__DisplayClassb.streakName == "osprey_gunner" || <>c__DisplayClassb.streakName == "remote_uav" || <>c__DisplayClassb.streakName == "remote_tank"))
			{
				ent.SetField("laptopWait", "timeout");
				this.initRideKillstreak2(ent);
				return;
			}
			ent.SetField("laptopWait", "get");
			int counter = 0;
			ent.OnInterval(100, delegate(Entity entity)
			{
				counter++;
				if (entity == null)
				{
					return false;
				}
				if (counter > 10 || entity.GetField<string>("laptopWait") != "get")
				{
					if (entity.GetField<string>("customStreak") == <>c__DisplayClassb.streakName)
					{
						entity.Call("SetPlayerData", new Parameter[]
						{
							"killstreaksState",
							"hasStreak",
							0,
							false
						});
						entity.Call("SetPlayerData", new Parameter[]
						{
							"killstreaksState",
							"icons",
							0,
							0
						});
						entity.SetField("customStreak", string.Empty);
					}
					<>c__DisplayClassb.<>4__this.initRideKillstreak2(entity);
					return false;
				}
				return true;
			});
		}
        private void initRideKillstreak2(Entity entity)
        {
            if (entity.GetField<string>("laptopWait") == "get")
            {
                entity.SetField("laptopWait", string.Empty);
            }
            else
            {
                if (entity.GetField<string>("laptopWait") == "weapon_switch_started")
                {
                    this.ClearUsingRemote(entity);
                    return;
                }
            }
            if (!entity.IsAlive || (entity.GetField<string>("laptopWait") == "death" && entity.GetField<string>("sessionteam") == "spectator"))
            {
                this.ClearUsingRemote(entity);
                return;
            }
            entity.Call("VisionSetNakedForPlayer", new Parameter[]
			{
				"black_bw",
				0.75f
			});
            int Count = 0;
            entity.SetField("laptopWait", "get");
            entity.OnInterval(100, delegate(Entity player)
            {
                Count++;
                if (player == null)
                {
                    return false;
                }
                if (Count <= 8 && !(player.GetField<string>("laptopWait") != "get"))
                {
                    return true;
                }
                Missile.clearRideIntro(player, 1f);
                if (player.GetField<string>("sessionteam") == "spectator")
                {
                    this.ClearUsingRemote(entity);
                    return false;
                }
                this.FirePredator(player);
                return false;
            });
        }
        private static void clearRideIntro(Entity ent, float delay = 0f)
        {
            if ((double)delay >= 0.1)
            {
                ent.AfterDelay(Convert.ToInt32(delay * 1000f), delegate(Entity entity)
                {
                    entity.Call("VisionSetNakedForPlayer", new Parameter[]
					{
						string.Empty,
						0
					});
                });
                return;
            }
            ent.Call("VisionSetNakedForPlayer", new Parameter[]
			{
				string.Empty,
				0
			});
        }
        public void tryUsePredator(Entity ent)
        {
            Missile.SetUsingRemote(ent, "remotemissile");
            this.initRideKillstreak(ent, "predator_missile");
        }
        private void FirePredator(Entity ent)
        {
            Entity entity = null;
            Vector3 v = Vector3.RandomXY();
            Vector3 vector = Vector3.RandomXY();
            if (entity != null)
            {
                Entity field = entity.GetField<Entity>("targetEnt");
                vector = field.Origin;
                Vector3 vector2 = base.Call<Vector3>("vectorNormalize", new Parameter[]
				{
					entity.Origin,
					vector
				});
                v = vector2 * (float)this.missileRemoteLaunchVert + vector;
            }
            else
            {
                Vector3 vector3 = base.Call<Vector3>("AnglesToForward", new Parameter[]
				{
					ent.GetField<Vector3>("angles")
				});
                v = ent.Origin + vector3 * -1f * (float)this.missileRemoteLaunchHorz + new Vector3(0f, 0f, (float)this.missileRemoteLaunchVert);
                vector = ent.Origin + vector3 * (float)this.missileRemoteLaunchTargetDist;
            }
            Entity entity2 = base.Call<Entity>("MagicBullet", new Parameter[]
			{
				"remotemissile_projectile_mp",
				v,
				vector,
				ent
			});
            if (entity2 == null)
            {
                this.ClearUsingRemote(ent);
                return;
            }
            entity2.Call("setCanDamage", new Parameter[]
			{
				true
			});
            this.MissileEyes(ent, entity2);
        }
        private void MissileEyes(Entity player, Entity rocket)
        {
            player.Call("VisionSetMissilecamForPlayer", new Parameter[]
			{
				"black_bw",
				0f
			});
            if (rocket == null)
            {
                this.ClearUsingRemote(player);
            }
            this.RidingPred.Add(player);
            player.Call("VisionSetMissilecamForPlayer", new Parameter[]
			{
				this.GetThermalVision(),
				1f
			});
            player.AfterDelay(150, delegate(Entity ent)
            {
                ent.Call("ThermalVisionFOFOverlayOn", new Parameter[0]);
            });
            player.Call("CameraLinkTo", new Parameter[]
			{
				rocket,
				"tag_origin"
			});
            player.Call("ControlsLinkTo", new Parameter[]
			{
				rocket
			});
            if (base.Call<int>("getdvarint", new Parameter[]
			{
				"camera_thirdPerson"
			}) == 1)
            {
                Missile.setThirdPersonDOF(player, false);
            }
            rocket.OnNotify("death", delegate(Entity _rocket)
            {
                if (this.RidingPred.Contains(player))
                {
                    this.RidingPred.Remove(player);
                }
                player.Call("ControlsUnlink", new Parameter[0]);
                player.Call("freezeControls", new Parameter[]
				{
					true
				});
                if (!this.GameEnded)
                {
                    Missile.staticEffect(player, 0.5f);
                }
                this.AfterDelay(500, delegate
                {
                    player.Call("ThermalVisionFOFOverlayOff", new Parameter[0]);
                    player.Call("CameraUnlink", new Parameter[0]);
                    if (this.Call<int>("getdvarint", new Parameter[]
					{
						"camera_thirdPerson"
					}) == 1)
                    {
                        Missile.setThirdPersonDOF(player, true);
                    }
                    if (this.isKillstreakWeapon(player.CurrentWeapon))
                    {
                        player.TakeWeapon(player.CurrentWeapon);
                        player.Call("SwitchToWeapon", new Parameter[]
						{
							player.GetField<string>("lastDroppableWeapon")
						});
                    }
                    this.ClearUsingRemote(player);
                });
            });
        }
        private static void staticEffect(Entity ent, float duration)
        {
            HudElem staticBG = HudElem.NewClientHudElem(ent);
            staticBG.HorzAlign = "fullscreen";
            staticBG.VertAlign = "fullscreen";
            staticBG.SetShader("white", 640, 480);
            staticBG.Archived = true;
            staticBG.Sort = 10;
            HudElem _static = HudElem.NewClientHudElem(ent);
            _static.HorzAlign = "fullscreen";
            _static.VertAlign = "fullscreen";
            _static.SetShader("ac130_overlay_grain", 640, 480);
            _static.Archived = true;
            _static.Sort = 20;
            ent.AfterDelay(Convert.ToInt32(duration * 1000f), delegate(Entity entity)
            {
                staticBG.Call("destroy", new Parameter[0]);
                _static.Call("destroy", new Parameter[0]);
            });
        }
    }
}