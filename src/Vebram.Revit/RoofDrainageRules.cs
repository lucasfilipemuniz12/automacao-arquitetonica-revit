namespace Vebram.Revit;
public sealed record RoofDrainageChoice(string Key,double? Slope,string Flow);
public sealed record RoofDrainageValue(double? Slope,string Flow,bool FromModel);
public static class RoofDrainageRules
{
 public static RoofDrainageValue Resolve(double modelSlope,string modelFlow,RoofDrainageChoice? choice)
 {
  if(!double.IsFinite(modelSlope)||modelSlope<0)throw new ArgumentException("Inclinação do modelo inválida.");
  if(modelSlope>=.05)return new(modelSlope,modelFlow,true);
  if(choice==null||choice.Slope==null)return new(null,"Modelo",false);
  if(!double.IsFinite(choice.Slope.Value)||choice.Slope<=0||choice.Slope>100)throw new ArgumentException("Informe caimento maior que zero e até 100%, ou deixe vazio.");
  if(choice.Flow is not ("Direita" or "Esquerda" or "Acima" or "Abaixo"))throw new ArgumentException("Escolha a direção para o caimento de projeto.");
  return new(choice.Slope,choice.Flow,false);
 }
}
