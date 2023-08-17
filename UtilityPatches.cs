using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Network;
using StardewValley.Events;
using StardewValley.BellsAndWhistles;
using System.Linq;

namespace KediNPCUtilities 
{
    public class UtilityPatches
    {
        public static bool CheckNPCUtility(string feature, string NPCname)
        {
            if (ModEntry.UtilityData.ContainsKey(NPCname))
            {
                foreach (var item in ModEntry.UtilityData[NPCname])
                    if (item.Key == feature)
                        return true;
            }
            return false;
        }
        public static string getParameterValue(string feature, string NPCname)
        {
            if (ModEntry.UtilityData.ContainsKey(NPCname))
            {
                foreach (var item in ModEntry.UtilityData[NPCname])
                    if (item.Key == feature)
                        return item.Value;
            }
            return "";
        }
        public static int VanillaWayOfFrame(string name) //Why. Just why. <.<
        {
            return name switch
            {
                "Emily" or "Abigail" => 33,
                "Alex" => 42,
                "Elliott" or "Penny" => 35,
                "Harvey" => 31,
                "Leah" => 25,
                "Sam" => 36,
                "Sebastian" => 40,
                "Shane" => 34,
                "Krobus" => 16,
                _ => 28,
            };
        }
        public static int DirectionBasedOnSpouseName(string name) //<.<
        {
            return name switch
            {
                "Abigail" or "Emily" or "Elliott" or "Harvey" or "Maru" or "Sebastian" or "Shane" or "Krobus" => 3,
                _ => 1,
            };
        }
        public static void tryToReceiveActiveObject_Prefix(Farmer who, NPC __instance)
        {
            if (who.ActiveObject.ParentSheetIndex == 460 && CheckNPCUtility("datableNotMarriable", __instance.Name))
            {
                if (who.friendshipData.TryGetValue(__instance.Name, out Friendship friendship) && friendship?.Status == FriendshipStatus.Dating)
                {
                    who.changeFriendship(-20, __instance);
                    return;
                }
            }
            else if (who.friendshipData.TryGetValue(__instance.Name, out Friendship friendship) && friendship is not null && __instance.Age != 2) //Stay away from children, creepers!
            {
                if (getParameterValue("marriageItem", __instance.Name).Split(",", StringSplitOptions.TrimEntries).Contains(who.ActiveObject.ParentSheetIndex.ToString()) || getParameterValue("platonicItem", __instance.Name).Split(",", StringSplitOptions.TrimEntries).ToList().Contains(who.ActiveObject.ParentSheetIndex.ToString()))
                {
                    var method = ModEntry.Helper.Reflection.GetMethod(__instance, "engagementResponse");
                    method.Invoke(new object[2] { who, getParameterValue("platonicItem", __instance.Name).Split(",", StringSplitOptions.TrimEntries).ToList().Contains(who.ActiveObject.ParentSheetIndex.ToString()) });
                }
                else if (getParameterValue("dateItem", __instance.Name).Split(",", StringSplitOptions.TrimEntries).ToList().Contains(who.ActiveObject.ParentSheetIndex.ToString()))
                {
                    Game1.player.friendshipData[__instance.Name].Status = FriendshipStatus.Dating;
                }
                else if (getParameterValue("breakupItem", __instance.Name).Split(",", StringSplitOptions.TrimEntries).ToList().Contains(who.ActiveObject.ParentSheetIndex.ToString()))
                {
                    Game1.player.friendshipData[__instance.Name].Status = FriendshipStatus.Friendly;
                    Game1.player.friendshipData[__instance.Name].Points -= 150;
                }
                else if (getParameterValue("divorceItem", __instance.Name).Split(",", StringSplitOptions.TrimEntries).ToList().Contains(who.ActiveObject.ParentSheetIndex.ToString()))
                {
                    who.divorceTonight.Value = true;
                    if (Game1.player.Money >= 50000)
                        Game1.player.Money -= 50000;
                    else
                    {
                        if (!Game1.player.modData.ContainsKey("KediDili.KNU.divorceDebt"))
                            Game1.player.modData.Add("KediDili.KNU.divorceDebt", (50000 - Game1.player.Money).ToString());

                        else if (Game1.player.modData["KediDili.KNU.divorceDebt"] == "0")
                            Game1.player.modData["KediDili.KNU.divorceDebt"] = (50000 - Game1.player.Money).ToString();

                        else if (Convert.ToInt32(Game1.player.modData["KediDili.KNU.divorceDebt"]) > 0)
                            Game1.player.modData["KediDili.KNU.divorceDebt"] = (Convert.ToInt32(Game1.player.modData["KediDili.KNU.divorceDebt"]) + (50000 - Game1.player.Money)).ToString();

                        Game1.player.Money = 0;
                    }
                }
                who.reduceActiveItemByOne();
                return;
            }
        }
        public static bool isGaySpouse_Prefix(NPC __instance, ref bool __result) 
        {
            if (CheckNPCUtility("alwaysAdopt", __instance.Name))
            {
                __result = true;
                return false;
            }
            else if (CheckNPCUtility("alwaysPregnant", __instance.Name))
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static bool canGetPregnant_Prefix(NPC __instance, ref bool __result)
        {
            if (CheckNPCUtility("alwaysAdopt", __instance.Name))
            {
                __result = false;
                return false;
            }
            else if (CheckNPCUtility("alwaysPregnant", __instance.Name))
            {
                __result = true;
                return false;
            }
            return true;
        }
        public static bool checkAction_Prefix(Farmer who, GameLocation l, NPC __instance, ref bool __result)
        {
            if (CheckNPCUtility("sidedInteractionFrame", __instance.Name))
            {
                if (__instance.Sprite.CurrentAnimation == null && !__instance.hasTemporaryMessageAvailable() && __instance.currentMarriageDialogue.Count == 0 && __instance.CurrentDialogue.Count == 0 && Game1.timeOfDay < 2200 && !__instance.isMoving() && who.ActiveObject == null)
                {
                    if (__instance.FacingDirection == 3 || __instance.FacingDirection == 1)
                    {
                        int spouseFrame = 0;

                        __instance.faceGeneralDirection(who.getStandingPosition(), 0, false, false);
                        who.faceGeneralDirection(__instance.getStandingPosition(), 0, false, false);

                        if (__instance.FacingDirection == DirectionBasedOnSpouseName(__instance.Name))
                            spouseFrame = VanillaWayOfFrame(__instance.Name);
                        else
                            spouseFrame = Convert.ToInt32(getParameterValue("sidedInteractionFrame", __instance.Name));

                        if (who.getFriendshipHeartLevelForNPC(__instance.Name) > 9 && __instance.sleptInBed.Value)
                        {
                            int delay = __instance.movementPause = Game1.IsMultiplayer ? 1000 : 10;
                            __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                            {
                                 new FarmerSprite.AnimationFrame(spouseFrame, delay, secondaryArm: false, false, __instance.haltMe, behaviorAtEndOfFrame: true)
                            });
                        }
                        if (!__instance.hasBeenKissedToday.Value)
                        {
                            who.changeFriendship(10, __instance);
                            l.playSound("dwop", NetAudio.SoundContext.NPC);
                            who.exhausted.Value = false;

                            __instance.hasBeenKissedToday.Value = true;
                            __instance.Sprite.UpdateSourceRect();
                        }
                        else
                        {
                            __instance.faceDirection((Game1.random.NextDouble() < 0.5) ? 2 : 0);
                            __instance.doEmote(12);
                        }
                        int playerFaceDirection = 1;
                        if (__instance.FacingDirection == 1)
                        {
                            playerFaceDirection = 3;
                        }
                        who.PerformKiss(playerFaceDirection);
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool setUp_QuestionEvent_Prefix(QuestionEvent __instance, ref bool __result, int ___whichQuestion)
        {
            if (___whichQuestion is 1 && (CheckNPCUtility("alwaysPregnant", Game1.player.spouse) || CheckNPCUtility("alwaysAdopt", Game1.player.spouse)))
            {
                Response[] answers;
                answers = new Response[2]
                {
                new Response("Yes", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_Yes")),
                new Response("Not", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_No"))
                };
                var method = ModEntry.Helper.Reflection.GetMethod(__instance, "answerPregnancyQuestion");

                Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Events:HaveBabyQuestion" + (CheckNPCUtility("alwaysAdopt", Game1.player.spouse) ? "_Adoption" : ""), Game1.player.Name), answers, method.MethodInfo.CreateDelegate<GameLocation.afterQuestionBehavior>(), Game1.getCharacterFromName(Game1.player.spouse));
                Game1.messagePause = true;
                __result = false;
                return false;
            }
            return true;
        }

        public static bool setUp_Prefix(ref bool __result, ref bool ___isMale, ref string ___message)
        {
            if (CheckNPCUtility("alwaysPregnant", Game1.player.spouse) || CheckNPCUtility("alwaysAdopt", Game1.player.spouse))
            {
                Random r = new((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed);
                NPC spouse = Game1.getCharacterFromName(Game1.player.spouse);
                Game1.player.CanMove = false;

                ___isMale = Game1.player.getNumberOfChildren() == 0 ? (r.NextDouble() < 0.5) : (Game1.player.getChildren()[0].Gender == 1);
                ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_" + (CheckNPCUtility("alwaysPregnant", Game1.player.spouse) ? "Adoption" : "SpouseMother"), Lexicon.getGenderedChildTerm(___isMale), spouse.displayName);

                __result = false;
                return false;
            }
            return true;
        }
        public static bool shouldPortraitShake_Prefix(ref bool __result, Dialogue d)
        {
            if (CheckNPCUtility("shakePortraits", d.speaker.Name))
            {
                int view = d.getPortraitIndex();

                foreach (var item in ModEntry.UtilityData[d.speaker.Name])
                {
                    if (item.Key == "shakePortraits")
                    {
                        string[] indexes = item.Value.Split(",", StringSplitOptions.TrimEntries);
                        __result = indexes.ToList().Contains(view.ToString());
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
/*

datableNotMarriable,    //Will always reject mermaid pendant
noChildren,             //Will never ask for children
sidedInteractionFrame,  //Different right and left kiss/hug frames
alwaysPregnant,         //This NPC should be always the pregnant one, regardless of player's gender.
alwaysAdopt,            //All farmers married to this NPC will only be able to adopt children, regardless of farmer's gender.
customProposalItem,     //This NPC can have other items for proposing to. Takes two parameters: Type(string), ItemID(int)
shakePortraits,          //Shakes portraits with specified indexes when they are displayed

 * Keep in mind that some keys are reserved for spesific purposes:
 * marriageProposal
 * platonicProposal
 * dateProposal
 * breakupProposal
 * divorceProposal
 * 
 * marriageItem
 * platonicItem
 * dateItem
 * breakupItem
 * divorceItem
 * 
 * datableNotMarriable
 * noChildren
 * sidedInteractionFrame
 * alwaysPregnant
 * alwaysAdopt
 * shakePortraits
 */
