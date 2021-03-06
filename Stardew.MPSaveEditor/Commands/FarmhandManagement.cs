using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using StardewValley.MPSaveEditor.Models;
using StardewValley.MPSaveEditor.Helpers;

namespace StardewValley.MPSaveEditor.Commands
{
    [Command(Name = "FarmhandManagement", Description = "Farmhand Management System. Manage all farmhands saved and stored.", ThrowOnUnexpectedArgument = false)]
    public class FarmhandManagementCommand {
        public string saveFilePath { get; set; }
        
        private SaveGame _game {get;set;}
        private Farmhands _farmhands {get; set;}     

        public bool SelectProgram(SaveGame game, out bool done) {
            _game = game;
            done = false;
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Farmhand Management System (FMS)");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Select a farmhand program: ");
            foreach(var command in Commands) {
                Console.WriteLine(string.Format("{0}. {1}", command.Key, command.Value));
            }
            var subProgram = Prompt.GetInt("Choose a program", 0);
            _farmhands =  new Farmhands(game);
            if (subProgram == 0) {
                return ShowCurrentFarmhands();
            }
            if (subProgram == 1) {
                return AddFarmhand();
            }
            if (subProgram == 2) {
                return RemoveFarmhand();
            }
            if (subProgram == 3) {
                return SwitchFarmhandByHost();
            }
            if (subProgram == 4) {               
                return SwitchFarmhandByStorage();
            }
            if(subProgram == 5) {
                done = true;
                return true;
            }

            return false;
        } 

        public bool ShowCurrentFarmhands() {
                Console.WriteLine("Current farmhands in game: ");
                Console.WriteLine(" >> Host: " + _game.Host.Element("name")?.Value ?? "None?!");
                SelectFarmhands(0, inGame:true, hideBlank:false);
                Console.WriteLine("Current farmhands in storage: ");
                SelectFarmhands(0, inGame:false, hideBlank:true);
                return true;
        }
        public bool AddFarmhand() {
            Console.WriteLine("---------");
            Console.WriteLine("Choose a farmhand: ");
            Console.WriteLine("1. <New Farmhand> (with cabin and player slot)");
            bool success = true;
            var farmhands = SelectFarmhands(1, inGame:false, hideBlank:true);
            var farmhandNumber = Prompt.GetInt(Purpose[PurposeEnum.AddToGame], 1);
            if (farmhandNumber == 1) {
                Console.WriteLine("Cabin Types:");
                Console.WriteLine("1. Log Cabin");
                Console.WriteLine("2. Stone Cabin");
                Console.WriteLine("3. Plank Cabin");
                Console.WriteLine("4. Random");
                var cabinType = Prompt.GetInt("Select a cabin type for the new cabin:", 4);
                Console.WriteLine("Adding new cabin and player slot...");
                success = _farmhands.AddFarmhand(cabinType);
                if (!success) {
                    Console.WriteLine("We had trouble finding a valid spot for the new cabin.");
                }
                _farmhands.SaveFile();
                CommandHelpers.SaveFile(_game);                                
            }
            if (farmhandNumber > 1) {
                var farmhand = farmhands.ElementAt(farmhandNumber - 2);
                success = _farmhands.AddFarmhand(farmhand);
                _farmhands.SaveFile();
                CommandHelpers.SaveFile(_game);   
            }
            return success;
        }

        public bool RemoveFarmhand() {
            Console.WriteLine("---------");            
            Console.WriteLine("Would you like to save the farmhand into storage for future use? ");
            Console.WriteLine("1. Yes");
            Console.WriteLine("2. No");
            var storeFarmhand = Prompt.GetInt("Store removed farmhand in storage? ", 1) == 1;
            var farmhands = SelectFarmhands(0, inGame:true, hideBlank:false);     
            var farmhandNumber = Prompt.GetInt(Purpose[storeFarmhand ? PurposeEnum.RemoveAndStore : PurposeEnum.RemoveNoStore], 1);
            var farmhand = farmhands.ElementAt(farmhandNumber - 1);
            var removeCabin = true;
            if (farmhand.Name != null) {
                Console.WriteLine("Do you also want to remove the cabin and player slot?");
                Console.WriteLine("1. Yes");
                Console.WriteLine("2. No");
                removeCabin = Prompt.GetInt("Remove cabin? ", 2) == 1;
            }
            _farmhands.RemoveFarmhandFromCabin(farmhand, storeFarmhand, removeCabin);
            
            _farmhands.SaveFile();
            CommandHelpers.SaveFile(_game);
            return true;
            
        }

        public bool SwitchFarmhandByHost() {
            Console.WriteLine("---------");
            Console.WriteLine("Choose a farmhand to change to the host of the saved game: ");
            Console.WriteLine(">> Host: " + _game.Host.Element("name")?.Value ?? "None?!");
            bool success = true;
            var farmhands = SelectFarmhands(0, inGame:true, hideBlank:true);
            var farmhandNumber = Prompt.GetInt(Purpose[PurposeEnum.SwitchHost], 1);  
            var farmhand = farmhands.ElementAt(farmhandNumber - 1);  
            _game.SwitchHost(farmhands.ElementAt(farmhandNumber - 1).Cabin);  
            CommandHelpers.SaveFile(_game);          
            return success;
        }

        public bool SwitchFarmhandByStorage() {
            if (!_farmhands.FarmhandsInStorage.Any()) {
                Console.WriteLine("No Farmhands found in storage for this save file.");
                return false;
            }
            Console.WriteLine("---------");
            Console.WriteLine("Choose the farmhands to switch: ");
            bool success = true;
            var farmhandsFromGame = SelectFarmhands(0, inGame:true, hideBlank:true);
            var farmhandNumber = Prompt.GetInt(Purpose[PurposeEnum.SwitchFromGame], 1);  
            var currentFarmhand = farmhandsFromGame.ElementAt(farmhandNumber - 1);  
            var farmhandsFromStorage = SelectFarmhands(0, inGame:false, hideBlank:true);
            farmhandNumber = Prompt.GetInt(Purpose[PurposeEnum.SwitchFromStorage], 1); 
            var newFarmhand = farmhandsFromStorage.ElementAt(farmhandNumber - 1); 
            Console.WriteLine($"Switching {currentFarmhand.Name} with {newFarmhand.Name}...");
            _farmhands.StoreFarmhand(currentFarmhand);                            
            currentFarmhand.Cabin.SwitchCabin(newFarmhand.Cabin);  
            CommandHelpers.SaveFile(_game);   
            _farmhands.RemoveFarmhandFromStorage(newFarmhand);   
            _farmhands.SaveFile();        
            return success;
        }

        // Returns a list of Farmhands, while also printing that list to the console
       public IEnumerable<Farmhand> SelectFarmhands(int startingIndex, bool? inGame,  bool hideBlank) {
            var farmhandCount = startingIndex;
            IEnumerable<Farmhand> farmhands; 
            if (inGame == true) {
                farmhands =  _farmhands.FarmhandsInGame;
            } else if (inGame == false) {
                farmhands = _farmhands.FarmhandsInStorage;
            } else {
                farmhands = _farmhands.AllFarmhands;
            }
            
            if (hideBlank) {
                farmhands = farmhands.Where(x => x.Name != null);
            }
            foreach (var fh in farmhands) {
                farmhandCount++;
                string farmhandID; 
                if (fh.Name is null) {
                    farmhandID = "<No Farmhand (Empty Cabin)>";
                } else {
                    farmhandID = string.Format("{0} - {1} Farm - Favorite Thing: {2}", fh.Name, fh.Farm, fh.FavoriteThing);
                }
                Console.WriteLine(string.Format("{0}. {1}", farmhandCount, farmhandID));        
            }
            return farmhands;
        }

        public int OnExecute() {
            try {
                saveFilePath = CommandHelpers.GetSaveFile(CommandHelpers.GetSaveFolder());
                var saveGame = new SaveGame(saveFilePath);
                bool done = false;
                bool result = false;
                while (!done) {
                    result = SelectProgram(saveGame, out done);
                    if (result) {
                        Console.Write("Done!");
                    } else {
                        Console.Write("Something went wrong...");
                    }
                    Console.ReadLine();
                }
                return result ? CommandHelpers.Success : CommandHelpers.Failure;
            } 
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return CommandHelpers.Failure;                  
            }
        }

        public Dictionary<int, string> Commands = new Dictionary<int, string> {
            {0, "Show current farmhands"},
            {1, "Add farmhand to game (or new player slot and cabin)"},
            {2, "Remove farmhand from game (or player slot and cabin)"},
            {3, "Switch farmhand with host of game"},
            {4, "Switch farmhand with stored farmhand"},
            {5, "Back"}
        };


        public Dictionary<PurposeEnum, string> Purpose = new Dictionary<PurposeEnum, string> {
            {PurposeEnum.AddToGame, "Select a farmhand to add to the game:"},
            {PurposeEnum.RemoveAndStore, "Select a farmhand to remove from the game and put in storage: "},
            {PurposeEnum.RemoveNoStore, "Select a farmhand to PERMANANTELY remove from the game: "},
            {PurposeEnum.SwitchFromGame, "Select a farmhand to switch from the game: "},
            {PurposeEnum.SwitchFromStorage, "Select a farmhand to switch from the storge: "},
            {PurposeEnum.SwitchHost, "Select a farmhand to switch with the host of the saved game:"}
        };

        public enum PurposeEnum {
            AddToGame,
            RemoveAndStore,
            RemoveNoStore,
            SwitchFromGame,
            SwitchFromStorage,
            SwitchHost,
        }
    }
}