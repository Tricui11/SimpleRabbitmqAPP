namespace FileParserService {
  public class Module {
    private static List<ModuleState> allStates;

    public string ModuleCategoryID { get; set; }
    public ModuleState ModuleState { get; set; }

    static Module() {
      allStates = Enum.GetValues(typeof(ModuleState)).Cast<ModuleState>().ToList();
    }

    public void ChangeModuleStateXml() {
      List<ModuleState> statesCopy = new(allStates);

      statesCopy.Remove(ModuleState);

      Random random = new();
      int index = random.Next(0, statesCopy.Count);
      ModuleState = statesCopy[index];
    }
  }
}
