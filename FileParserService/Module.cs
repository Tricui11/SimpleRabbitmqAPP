namespace FileParserService {
  public class Module {
    public string ModuleCategoryID { get; set; }
    public ModuleState ModuleState { get; set; }

    public static ModuleState ChangeModuleStateXml(ModuleState currentState) {
      var allStates = Enum.GetValues(typeof(ModuleState)).Cast<ModuleState>().ToList();

      allStates.Remove(currentState);

      Random random = new();
      int index = random.Next(0, allStates.Count);
      return allStates[index];
    }
  }
}
