using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.Rendering;using Object=UnityEngine.Object;
namespace HollowCreek.Editor.Props {
 public static class AliceHomeInteriorAuthoring {
 const string Dir="Assets/Art/Environment/UnifiedHouse/Interior";
 static Material wood,floor,plaster,linen,velvet,rug,leather,brass,ceramic,paper,green,wallpaper,tiles,steel,enamel;
 [MenuItem("Hollow Creek/Реквизит/Обжить дом Элис")]
 public static void Build(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit play mode");var h=GameObject.Find("[Location] Street").transform.Find("Alice house interior");if(PrefabUtility.IsAnyPrefabInstanceRoot(h.gameObject))PrefabUtility.UnpackPrefabInstance(h.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);Apply(h);SaveInterior(h);UnifiedHouseAuthoring.ApplyExisting(h.parent);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(h.gameObject.scene);EditorSceneManager.SaveScene(h.gameObject.scene);}
 public static void Apply(Transform h){
 System.IO.Directory.CreateDirectory(Dir);
 wood=Mat("AgedWalnut",new Color(.31f,.21f,.135f),0,.23f);floor=Mat("OldOakFloor",new Color(.37f,.28f,.19f),1,.29f);plaster=Mat("WarmAgedPlaster",new Color(.52f,.47f,.38f),2,.10f);
 linen=Mat("OatmealLinen",new Color(.52f,.45f,.32f),3,.12f);velvet=Mat("FadedForestUpholstery",new Color(.19f,.27f,.23f),3,.18f);rug=Mat("FadedBurgundyRug",new Color(.34f,.13f,.12f),4,.08f);leather=Mat("WornBrownLeather",new Color(.16f,.105f,.07f),5,.31f);brass=Mat("AgedBrass",new Color(.38f,.29f,.13f),6,.38f);ceramic=Mat("IvoryStoneware",new Color(.69f,.64f,.51f),2,.42f);paper=Mat("OldCreamPaper",new Color(.61f,.56f,.43f),2,.09f);green=Mat("PlantLeaves",new Color(.16f,.25f,.12f),3,.13f);
 wallpaper=Mat("VictorianSageDamask",new Color(.49f,.49f,.39f),7,.10f);tiles=Mat("KitchenCreamTiles",new Color(.69f,.66f,.54f),8,.45f);steel=Mat("BrushedKitchenSteel",new Color(.47f,.49f,.46f),6,.57f);enamel=Mat("OldIvoryEnamel",new Color(.67f,.66f,.57f),2,.51f);
 var geometry=h.Find("Geometry");foreach(var r in geometry.GetComponentsInChildren<Renderer>(true)){
 if(r.name=="Floor")Assign(r,floor);else if(r.name.Contains("Wall")||r.name.Contains("Partition"))Assign(r,wallpaper);else if(r.name=="Ceiling")Assign(r,plaster);else if(r.sharedMaterial&&r.sharedMaterial.name=="Wood")Assign(r,wood);
 }
 var art=h.Find("Art");foreach(var r in art.GetComponentsInChildren<Renderer>(true)){Retexture(r);if(r.name=="rugRounded")r.enabled=false;}var shelf=art.Find("bookcaseOpen");if(shelf){shelf.localRotation=Quaternion.identity;var rs=shelf.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);shelf.position+=h.TransformPoint(new Vector3(-4.23f,.012f,13.56f))-new Vector3(b.center.x,b.min.y,b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(shelf);}
 var hallPlant=art.Cast<Transform>().FirstOrDefault(t=>t.name=="pottedPlant"&&t.localPosition.z<5);if(hallPlant){hallPlant.localScale=Vector3.one;var rs=hallPlant.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);hallPlant.localScale=new Vector3(.70f/Mathf.Max(b.size.x,b.size.z),1.95f/b.size.y,.70f/Mathf.Max(b.size.x,b.size.z));b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);hallPlant.position+=h.TransformPoint(new Vector3(4.56f,.012f,4.54f))-new Vector3(b.center.x,b.min.y,b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(hallPlant);}
 foreach(var old in h.Cast<Transform>().Where(t=>t.name=="Inherited home furnishings").ToArray())Object.DestroyImmediate(old.gameObject);
 var coffee=art.Find("tableCoffee");if(coffee){var rs=coffee.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);coffee.position+=h.TransformPoint(new Vector3(3.15f,.012f,9.35f))-new Vector3(b.center.x,b.min.y,b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(coffee);}
 var root=Group(h,"Inherited home furnishings");
 Entry(root);SittingRoom(root);Study(root);KeepsakeDresser(root,h);Kitchen(root);Curtains(root);Sconces(root);SafeCorner(root);Trim(root,geometry);
 var studyLight=h.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name=="Study Light");if(studyLight){studyLight.intensity=5.6f;PrefabUtility.RecordPrefabInstancePropertyModifications(studyLight);}
 AssetDatabase.SaveAssets();
 }
 public static void SaveInterior(Transform h){var clone=Object.Instantiate(h.gameObject);clone.transform.SetParent(null);clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);clone.transform.localScale=Vector3.one;try{if(PrefabUtility.IsAnyPrefabInstanceRoot(clone))PrefabUtility.UnpackPrefabInstance(clone,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);PrefabUtility.SaveAsPrefabAsset(clone,"Assets/Art/Environment/UnifiedHouse/AliceHouseInterior.prefab");}finally{Object.DestroyImmediate(clone);}}
 static void Entry(Transform p){var g=Group(p,"Entry - daily belongings");
 Model(g,"benchCushion","Entry bench",new(-4.4f,.012f,2.6f),.82f,-90,1.35f,true);
 Model(g,"sideTableDrawers","Inherited hallway chest",new(3.7f,.012f,.39f),.85f,180,1.35f,true);
 Model(g,"lampRoundTable","Hallway table lamp",new(3.3f,.862f,.34f),.48f,0,0,false);
 Lamp(g,"Hallway lamp pool",new(3.3f,1.23f,.34f),.8f,3.4f);
 Rug(g,"Woven entry runner",new(0,.028f,2.6f),new(1.35f,.015f,3.4f));
 // Existing rack stands at (-4.1, 1); garments follow its arms.
 HangingCoat(g);
 var scarf=Group(g,"Scarf draped over bench");scarf.localPosition=new(-4.4f,.49f,2.45f);var s=new MeshKit();int m=s.Mat("cloth");int start=s.Sheet(new Vector2(.26f,.85f),8,25,m,m);s.Deform(start,v=>new Vector3(v.x,Mathf.Lerp(0,-.34f,Mathf.Clamp01((v.y+.425f)/.85f)),v.y));Mesh(scarf,"Soft folded scarf",s,new[]{Mat("RustScarf",new Color(.44f,.24f,.14f),3,.1f)});
 Shoes(g,new(-4.2f,.012f,3.27f),9,true);Shoes(g,new(-3.68f,.012f,3.29f),-12,false);
 Book(g,new(3.91f,.875f,.38f),new(.3f,.035f,.22f),17);Bowl(g,new(3.47f,.88f,.47f),.12f);
 Frame(g,"Old house photograph",new(4.40f,1.91f,.095f),new(.60f,.43f),0,"Photo_House");
 }
 static void SittingRoom(Transform p){var g=Group(p,"Front room - evening reading");
 Sofa(g,new(-2.95f,.012f,4.02f),0);
 Model(g,"tableCoffee","Low walnut coffee table",new(-2.9f,.012f,2.47f),.44f,0,1.4f,true);
 Rug(g,"Front sitting rug",new(-2.9f,.030f,2.9f),new(2.9f,.015f,2.5f));
 Book(g,new(-3.25f,.47f,2.55f),new(.31f,.05f,.23f),-13);Book(g,new(-3.24f,.525f,2.55f),new(.27f,.045f,.20f),-4);Cup(g,new(-2.58f,.47f,2.3f));
 Pillow(g,new(-3.73f,.62f,4.05f),-8,linen);Pillow(g,new(-2.19f,.62f,4.07f),11,rug);
 Model(g,"loungeChair","Second sitting chair",new(3.94f,.012f,3.74f),.93f,24,1.15f,true);
 Model(g,"sideTable","Chair side table",new(2.82f,.012f,3.60f),.57f,0,.78f,true);Cup(g,new(2.58f,.588f,3.65f));OpenReadingBook(g,new(2.95f,.586f,3.58f));HallReadingLamp(g);ChairThrow(g);
 Frame(g,"Framed cemetery photograph",new(-3.05f,1.85f,4.90f),new(.65f,.46f),180,"Photo_Cemetery");
 Lamp(g,"Reading floor lamp pool",new(-4.2f,1.42f,4.2f),.75f,3.8f);
 }
 static Vector3 ThrowPoint(float x,float t){t=Mathf.Clamp01(t);var profile=new[]{new Vector2(.42f,.975f),new Vector2(.235f,.975f),new Vector2(.235f,.76f),new Vector2(.235f,.49f),new Vector2(-.15f,.49f),new Vector2(-.50f,.49f),new Vector2(-.565f,.17f)};float f=t*(profile.Length-1);int i=Mathf.Min(profile.Length-2,Mathf.FloorToInt(f));var yz=Vector2.Lerp(profile[i],profile[i+1],f-i);float xx=x-.14f+.032f*Mathf.Sin(t*8);float fold=.012f*Mathf.Sin(x*28+t*17)+.006f*Mathf.Sin(x*57-t*6);float y=yz.y+fold+.034f;float onArm=Mathf.SmoothStep(0,1,Mathf.Clamp01((-xx-.30f)/.085f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((yz.x+.477f)/.05f));y=Mathf.Lerp(y,Mathf.Max(y,.783f+fold),onArm);return new Vector3(xx,y,yz.x+.011f*Mathf.Sin(x*21+t*13));}
 static Vector3 ThrowSurface(Vector3 v){float t=(v.y+.875f)/1.75f;var dx=ThrowPoint(v.x+.001f,t)-ThrowPoint(v.x-.001f,t);var dt=ThrowPoint(v.x,t+.001f)-ThrowPoint(v.x,t-.001f);return ThrowPoint(v.x,t)+Vector3.Cross(dx,dt).normalized*v.z;}
 static void ChairThrow(Transform p){var g=Group(p,"Soft throw casually draped over hallway chair");g.localPosition=new(3.94f,.012f,3.74f);g.localRotation=Quaternion.Euler(0,24,0);var k=new MeshKit();int cloth=k.Mat("pile"),start=k.Sheet(new Vector2(.74f,1.75f),40,70,cloth,cloth,thickness:.011f);k.Deform(start,ThrowSurface);var mat=Mat("SoftOatmealFleece",new Color(.66f,.62f,.54f),9,.08f);mat.SetFloat("_Cull",2);EditorUtility.SetDirty(mat);Mesh(g,"Folded fluffy reading throw",k,new[]{mat});
 var pile=new MeshKit();int fibres=pile.Mat("fleece");for(int row=0;row<20;row++)for(int col=0;col<34;col++){float seed=Mathf.Sin((row*73+col*19)*12.71f)*.5f+.5f;float x=-.354f+(col+seed*.35f)*.021f,t=(row+.3f+seed*.3f)/20f;var dx=ThrowPoint(x+.001f,t)-ThrowPoint(x-.001f,t);var dt=ThrowPoint(x,t+.001f)-ThrowPoint(x,t-.001f);var normal=Vector3.Cross(dx,dt).normalized;var tangent=dx.normalized;var pos=ThrowPoint(x,t)+normal*.006f;pile.Sweep(new[]{pos,pos+normal*.004f+tangent*.001f,pos+normal*(.005f+seed*.002f)+tangent*.002f},new Vector2(.0008f,.0008f),fibres);}Mesh(g,"Soft curled fleece pile",pile,new[]{Mat("LightFleeceFibres",new Color(.72f,.68f,.60f),9,.05f)});
 var fringe=new MeshKit();int m=fringe.Mat("wool");for(int i=0;i<23;i++){float x=-.36f+i*.032f;var pos=ThrowPoint(x,1);fringe.Sweep(new[]{pos,pos+new Vector3(.004f,-.018f,-.005f),pos+new Vector3(-.002f,-.049f,-.011f),pos+new Vector3(.003f,-.066f,-.011f)},new Vector2(.0035f,.004f),m);}Mesh(g,"Loose wool fringe",fringe,new[]{mat});}
 static float PageY(float x,float z)=>.008f+.020f*(1-Mathf.Abs(x)/.175f)+.0025f*Mathf.Sin(z*23+x*13);
 static void OpenReadingBook(Transform p,Vector3 pos){var g=Group(p,"Open book beside hallway armchair");g.localPosition=pos;g.localRotation=Quaternion.Euler(0,-8,0);var k=new MeshKit();int covers=k.Mat("leather"),pages=k.Mat("paper"),ink=k.Mat("ink"),ribbon=k.Mat("ribbon");
 foreach(float sign in new[]{-1f,1f}){k.Box(new(sign*.091f,.002f,0),new(.184f,.006f,.228f),covers,.003f);k.Box(new(sign*.089f,.009f,0),new(.174f,.010f,.215f),pages,.002f);var ids=new int[21,17];for(int ix=0;ix<=20;ix++)for(int iz=0;iz<=16;iz++){float x=sign*Mathf.Lerp(.010f,.175f,ix/20f),z=Mathf.Lerp(-.105f,.105f,iz/16f);ids[ix,iz]=k.Vertex(new(x,PageY(x,z)+.007f,z),Vector3.up,new Vector2(ix/20f,iz/16f));}for(int ix=0;ix<20;ix++)for(int iz=0;iz<16;iz++)k.Quad(pages,ids[ix,iz],ids[ix+1,iz],ids[ix+1,iz+1],ids[ix,iz+1]);
 for(int line=0;line<15;line++){float z=-.084f+line*.0109f;float xa=sign*.026f,xb=sign*(.148f-(line%5)*.004f);int a=k.Vertex(new(xa,PageY(xa,z)+.0077f,z),Vector3.up,Vector2.zero),b=k.Vertex(new(xb,PageY(xb,z)+.0077f,z),Vector3.up,Vector2.right),c=k.Vertex(new(xb,PageY(xb,z+.001f)+.0077f,z+.001f),Vector3.up,Vector2.one),d=k.Vertex(new(xa,PageY(xa,z+.001f)+.0077f,z+.001f),Vector3.up,Vector2.up);k.Quad(ink,a,b,c,d);}}
 k.Box(new(0,.015f,0),new(.013f,.03f,.23f),covers,.004f);k.Sweep(new[]{new Vector3(.016f,.034f,.06f),new Vector3(.021f,.033f,.108f),new Vector3(.025f,.009f,.13f),new Vector3(.035f,-.025f,.145f)},new Vector2(.012f,.0012f),ribbon);Mesh(g,"Open curved pages and bookmark",k,new[]{leather,paper,Mat("BookPrintingInk",new Color(.20f,.16f,.115f),3,.02f),rug});}
 static void HallReadingLamp(Transform p){var g=Group(p,"Adjustable hallway reading floor lamp");g.localPosition=new(3.02f,.012f,4.50f);var iron=Mat("ReadingLampGraphite",new Color(.09f,.095f,.085f),6,.29f);var k=new MeshKit();int black=k.Mat("graphite"),metal=k.Mat("steel");k.Cylinder(new(0,.027f,0),.19f,.055f,black,32,bevel:.012f);k.Cylinder(new(0,.50f,0),.015f,.95f,black,20);k.Cylinder(new(0,.79f,0),.023f,.23f,metal,20);var joints=new[]{new Vector3(0,1.0f,0),new Vector3(.14f,1.48f,-.15f),new Vector3(.38f,1.45f,-.34f)};k.Sweep(joints,new Vector2(.026f,.026f),black);foreach(var v in joints){k.Sphere(v,.035f,metal,20);k.Box(v+Vector3.right*.043f,new(.032f,.017f,.017f),black,.006f);}Mesh(g,"Articulated reading lamp stand",k,new[]{iron,steel});var baseCollider=g.gameObject.AddComponent<BoxCollider>();baseCollider.center=new(0,.04f,0);baseCollider.size=new(.38f,.08f,.38f);
 var head=Group(g,"Adjustable reading head");head.localPosition=joints[2];Vector3 direction=new Vector3(.82f,.46f,-.95f)-joints[2];head.localRotation=Quaternion.FromToRotation(Vector3.down,direction.normalized);var shade=new MeshKit();int outer=shade.Mat("graphite"),glow=shade.Mat("diffuser");shade.Lathe(new[]{new Vector2(.115f,0),new Vector2(.11f,.025f),new Vector2(.054f,.16f),new Vector2(0,.17f)},outer,32);shade.Disc(new(0,-.001f,0),.105f,glow,32,up:false);var shadeGo=Mesh(head,"Directional reading lamp shade",shade,new[]{iron,CandleGlow("ReadingLampWarmDiffuser",new Color(1,.78f,.47f),1.4f)});shadeGo.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;var light=Group(head,"Warm light aimed at chair");light.localPosition=new(0,-.023f,0);light.localRotation=Quaternion.Euler(90,0,0);var l=light.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(1,.80f,.57f);l.intensity=2.5f;l.range=4;l.spotAngle=64;l.innerSpotAngle=38;l.shadows=LightShadows.Soft;l.shadowBias=.04f;l.shadowNormalBias=.12f;
 }
 static void Study(Transform p){var g=Group(p,"Study - family belongings");
 Model(g,"bookcaseClosedWide","Old family cabinet",new(-2.4f,.012f,13.58f),1.72f,0,2.0f,true);
 for(int i=0;i<5;i++)Model(g,"books","Books in tall shelf "+i,new(-4.40f+(i/3)*.39f,.32f+(i%3)*.49f,13.45f),.23f,0,.38f,false);
 for(int row=0;row<3;row++)for(int col=0;col<3;col++)Model(g,"books","Inherited family books "+row+" "+col,new(-3.06f+col*.56f,.13f+row*.48f,13.40f),.23f,0,.38f,false);
 for(int i=0;i<3;i++)Model(g,"books","Books on low shelf "+i,new(-4.52f,.24f+i*.32f,8.14f),.23f,90,.36f,false);
 Model(g,"loungeChair","Study reading chair",new(2.55f,.012f,10.7f),.98f,-25,1.18f,true);
 Model(g,"lampRoundFloor","Study reading lamp",new(1.58f,.012f,11.72f),1.63f,0,0,true);Lamp(g,"Study warm lamp pool",new(1.58f,1.47f,11.72f),.88f,4.2f);
 Rug(g,"Study reading rug",new(2.7f,.035f,10.75f),new(2.15f,.014f,2.5f));
 Pillow(g,new(2.73f,.62f,11.02f),-19,linen);
 Model(g,"sideTable","Reading side table",new(1.53f,.012f,10.45f),.57f,0,.6f,true);Book(g,new(1.53f,.6f,10.4f),new(.27f,.035f,.19f),22);Cup(g,new(1.66f,.64f,10.54f));
 Cup(g,new(-2.93f,.797f,9.68f));Lamp(g,"Desk lamp pool",new(-3.1f,1.15f,10.25f),.65f,2.8f);
 var trunk=Group(g,"Small inherited leather trunk");trunk.localPosition=new(-.8f,.012f,13.52f);Box(trunk,"Leather trunk body",new(0,.22f,0),new(.85f,.44f,.48f),leather,.055f,true);Box(trunk,"Trunk lid",new(0,.46f,0),new(.87f,.07f,.5f),leather,.025f);
 foreach(float x in new[]{-.28f,.28f}){Box(trunk,"Brass band",new(x,.24f,-.244f),new(.026f,.40f,.008f),brass,.003f);Box(trunk,"Latch",new(x,.35f,-.26f),new(.055f,.07f,.02f),brass,.008f);}
 CandleGroup(g,"Candles on family cabinet",new(-2.5f,1.735f,13.47f),2,1.15f);
 Frame(g,"Family house memory",new(-1.95f,2.1f,13.91f),new(.65f,.46f),180,"Photo_House");Frame(g,"Bridge memory",new(-3.2f,2.14f,13.91f),new(.52f,.65f),180,"Photo_Bridge");
 }
 static void KeepsakeDresser(Transform parent,Transform house){
 // Replace the shallow stand with a complete chest, keeping the interactive box itself.
 var old=house.Find("Art/sideTableDrawers");if(old){old.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);}
 var g=Group(parent,"Walnut keepsake dresser");g.localPosition=new(4.49f,.012f,9.0f);g.localRotation=Quaternion.Euler(0,90,0);
 foreach(float x in new[]{-.61f,.61f})foreach(float z in new[]{-.26f,.26f})Box(g,"Rounded walnut dresser foot",new(x,.07f,z),new(.11f,.14f,.11f),wood,.018f);
 Box(g,"Walnut dresser carcass",new(0,.48f,0),new(1.40f,.72f,.65f),wood,.012f,true);
 Box(g,"Dresser lower moulding",new(0,.14f,0),new(1.45f,.058f,.70f),wood,.013f);
 Box(g,"Dresser upper moulding",new(0,.845f,0),new(1.45f,.036f,.70f),wood,.009f);
 Box(g,"Solid aged walnut dresser top",new(0,.878f,0),new(1.50f,.05f,.74f),wood,.013f);
 var dark=Mat("DresserDrawerShadow",new Color(.085f,.058f,.035f),0,.12f);
 Box(g,"Dark drawer reveals",new(0,.493f,-.328f),new(1.31f,.65f,.011f),dark,.002f);
 for(int row=0;row<3;row++){float y=.283f+row*.213f;Box(g,"Inset walnut drawer front "+row,new(0,y,-.346f),new(1.28f,.190f,.035f),wood,.008f);Box(g,"Drawer raised timber panel "+row,new(0,y,-.369f),new(1.17f,.127f,.016f),wood,.005f);
 var k=new MeshKit();int m=k.Mat("brass");foreach(float x in new[]{-.37f,.37f}){using(k.At(new(x,y,-.388f),new(90,0,0)))k.Cylinder(Vector3.zero,.029f,.007f,m,24);k.Sweep(new[]{new Vector3(x-.038f,y,-.393f),new Vector3(x-.038f,y-.03f,-.421f),new Vector3(x+.038f,y-.03f,-.421f),new Vector3(x+.038f,y,-.393f)},new Vector2(.007f,.007f),m);}Mesh(g,"Tarnished brass drawer pulls "+row,k,new[]{brass});}
 const float top=.915f;
 PlaceSupportedProp(house,"Photo Box",new(4.48f,top,9.43f));PlaceSupportedProp(house,"Radio",new(4.59f,top,8.48f));
 CandleGroup(parent,"Candles on keepsake dresser",new(4.62f,top,8.98f),3,1.25f);parent.Find("Candles on keepsake dresser").localRotation=Quaternion.Euler(0,90,0);
 var notebook=Group(parent,"Cloth notebooks on dresser");notebook.localPosition=new(4.28f,top+.003f,8.64f);notebook.localRotation=Quaternion.Euler(0,83,0);
 Book(notebook,Vector3.zero,new(.24f,.028f,.17f),0);Book(notebook,new(.016f,.035f,-.005f),new(.22f,.023f,.16f),-9);
 var covers=Mat("FadedOliveNotebookCloth",new Color(.24f,.28f,.19f),3,.10f);foreach(var r in notebook.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Cloth cover"||r.name=="Book spine"))Assign(r,covers);
 Box(notebook,"Notebook paper title label",new(.016f,.062f,-.005f),new(.10f,.0015f,.052f),paper,.001f);
 var notes=Group(parent,"Handwritten notes and pencil");notes.localPosition=new(4.28f,top+.0015f,9.10f);notes.localRotation=Quaternion.Euler(0,78,0);
 Box(notes,"Loose cream stationery",Vector3.zero,new(.21f,.002f,.155f),paper,.001f);var ink=Mat("FadedWritingInk",new Color(.19f,.16f,.12f),3,.02f);
 for(int line=0;line<6;line++)for(int word=0;word<3;word++){float x=-.080f+word*.053f,z=-.052f+line*.017f;Box(notes,"Uneven handwritten line",new(x+.014f,.0018f,z),new(.025f+(line+word)%3*.004f,.0006f,.0007f),ink);}
 var pencil=Group(notes,"Used wooden pencil");pencil.localPosition=new(.046f,.008f,.024f);pencil.localRotation=Quaternion.Euler(90,0,-18);var pen=new MeshKit();int paint=pen.Mat("paint"),tip=pen.Mat("wood"),lead=pen.Mat("lead"),band=pen.Mat("brass"),eraser=pen.Mat("eraser");pen.Cylinder(Vector3.zero,.0037f,.14f,paint,6);pen.Lathe(new[]{new Vector2(.0037f,.07f),new Vector2(.0009f,.086f)},tip,6);pen.Lathe(new[]{new Vector2(.0009f,.086f),new Vector2(0,.091f)},lead,6);pen.Cylinder(new(0,-.069f,0),.004f,.012f,band,12);pen.Cylinder(new(0,-.080f,0),.004f,.01f,eraser,12);Mesh(pencil,"Hexagonal pencil with sharpened tip",pen,new[]{Mat("WornOchrePencilPaint",new Color(.55f,.35f,.12f),2,.2f),wood,ink,brass,Mat("FadedPencilEraser",new Color(.48f,.30f,.27f),3,.08f)});
 var dish=Group(parent,"Shallow stoneware key dish");dish.localPosition=new(4.28f,top,9.50f);var bowl=new MeshKit();bowl.Lathe(new[]{new Vector2(0,.005f),new Vector2(.044f,.005f),new Vector2(.052f,.012f),new Vector2(.075f,.029f),new Vector2(.072f,.034f),new Vector2(.047f,.014f),new Vector2(0,.014f)},bowl.Mat("glaze"),32);Mesh(dish,"Soft glazed ceramic key dish",bowl,new[]{ceramic});var keys=new MeshKit();int metal=keys.Mat("brass");keys.Torus(new(4.28f,top+.018f,9.50f),.018f,.002f,metal,20,8);for(int i=0;i<2;i++)using(keys.At(new(4.285f+i*.012f,top+.018f+i*.003f,9.515f),new(0,i*32-12,0))){keys.Torus(Vector3.zero,.008f,.002f,metal,16,6);keys.Box(new(0,0,.025f),new(.004f,.003f,.043f),metal,.001f);keys.Box(new(.004f,0,.042f),new(.011f,.003f,.005f),metal,.001f);}Mesh(parent,"Old keys in stoneware dish",keys,new[]{brass});
 }
 static void PlaceSupportedProp(Transform house,string name,Vector3 bottom){var t=house.Find("Props/"+name);if(!t)t=house.Find("Art/"+name.ToLowerInvariant());if(!t)return;var rs=t.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)return;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);t.position+=house.TransformPoint(bottom)-new Vector3(b.center.x,b.min.y,b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
 static void HangingCoat(Transform parent){
 var extension=new MeshKit();extension.Sweep(new[]{new Vector3(-3.877f,1.78f,1),new Vector3(-3.72f,1.765f,1),new Vector3(-3.62f,1.78f,1),new Vector3(-3.62f,1.805f,1)},new Vector2(.034f,.034f),extension.Mat("wood"));Mesh(parent,"Extended right coat hook",extension,new[]{wood});
 var coat=Group(parent,"Coat hanging vertically from right hook");coat.localPosition=new(-3.62f,0,1);coat.localRotation=Quaternion.Euler(0,90,0);
 var cloth=Mat("SlateWoolCoat",new Color(.29f,.32f,.33f),3,.1f);var kit=new MeshKit();int wool=kit.Mat("wool"),metal=kit.Mat("metal");
 // A rounded, open-front cloth shell with folds hangs away from the central pole.
 const int rings=26,sides=40;var indices=new int[rings+1,sides+1];
 for(int row=0;row<=rings;row++){float t=(float)row/rings,y=Mathf.Lerp(.62f,1.69f,t);float width=t<.72f?Mathf.Lerp(.225f,.18f,t/.72f):Mathf.Lerp(.18f,.075f,(t-.72f)/.28f);width+=.065f*Mathf.Exp(-Mathf.Pow((t-.90f)/.055f,2));
 for(int col=0;col<=sides;col++){float angle=Mathf.Lerp(Mathf.PI*.5f+.16f,Mathf.PI*2.5f-.16f,(float)col/sides);float fold=Mathf.Sin(angle*9+t*.9f)*.010f*(1-t*.75f);float x=Mathf.Cos(angle)*(width+fold);float z=Mathf.Sin(angle)*(.081f+fold)+.035f*(1-t);indices[row,col]=kit.Vertex(new(x,y,z),new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)).normalized,new Vector2((float)col/sides,t));}
 }
 for(int row=0;row<rings;row++)for(int col=0;col<sides;col++)kit.Quad(wool,indices[row,col],indices[row+1,col],indices[row+1,col+1],indices[row,col+1]);
 foreach(float sign in new[]{-1f,1f}){var arm=new int[17,25];for(int row=0;row<=16;row++){float t=(float)row/16;float y=1.54f-t*.72f,x=sign*(.218f+.061f*Mathf.Sin(t*2.7f)),z=t*.065f,r=Mathf.Lerp(.072f,.045f,t);for(int col=0;col<=24;col++){float angle=col*Mathf.PI/12;float fold=.004f*Mathf.Sin(angle*5+t*4);arm[row,col]=kit.Vertex(new(x+Mathf.Cos(angle)*(r+fold),y,z+Mathf.Sin(angle)*(r+fold)),new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),new Vector2((float)col/24,t));}}
 for(int row=0;row<16;row++)for(int col=0;col<24;col++)kit.Quad(wool,arm[row,col],arm[row+1,col],arm[row+1,col+1],arm[row,col+1]);
 using(kit.At(new(sign*.195f,1.55f,0),scale:new Vector3(1.15f,.65f,.87f)))kit.Sphere(Vector3.zero,.085f,wool,24);
 using(kit.At(new(sign*.055f,1.55f,.092f),new(0,0,sign*-13)))kit.Extrude(new[]{new Vector2(-.025f,-.10f),new Vector2(.036f,.095f),new Vector2(-.025f,.075f)},.014f,wool);
 }
 kit.Lathe(new[]{new Vector2(.072f,1.653f),new Vector2(.080f,1.69f),new Vector2(.074f,1.703f),new Vector2(.063f,1.696f),new Vector2(.060f,1.660f)},wool,32);
 // Hanger and garment share one vertical plane below the extended side hook.
 kit.Sweep(new[]{new Vector3(-.011f,1.696f,0),new Vector3(-.011f,1.77f,0),new Vector3(-.008f,1.81f,0),new Vector3(.021f,1.825f,0),new Vector3(.040f,1.80f,0),new Vector3(.034f,1.769f,0)},new Vector2(.009f,.009f),metal);
 kit.Sweep(new[]{new Vector3(-.21f,1.573f,0),new Vector3(0,1.696f,0),new Vector3(.21f,1.573f,0),new Vector3(-.21f,1.573f,0)},new Vector2(.009f,.009f),metal);
 for(int i=0;i<4;i++){float y=1.40f-i*.155f,t=(y-.62f)/1.07f;kit.Sphere(new(.039f,y,.081f+.035f*(1-t)+.006f),.010f,metal,12);}
 var go=Mesh(coat,"Draped wool coat and hanger",kit,new[]{cloth,brass});
 // Render the thin cloth from inside too; its front opening is real geometry.
 var doubleSided=Mat("HangingWoolDoubleSided",new Color(.29f,.32f,.33f),3,.1f);doubleSided.SetFloat("_Cull",0);EditorUtility.SetDirty(doubleSided);go.GetComponent<Renderer>().sharedMaterials=new[]{doubleSided,brass};
 }
 static void SafeCorner(Transform parent){var g=Group(parent,"Safe corner - inherited keepsakes");
 Model(g,"sideTableDrawers","Safe corner sideboard",new(4.14f,.012f,13.48f),.88f,0,1.10f,true);
 Rug(g,"Small faded safe rug",new(2.75f,.029f,12.97f),new(2.38f,.013f,1.55f));
 Frame(g,"Old school photograph near safe",new(4.10f,1.96f,13.90f),new(.72f,.49f),180,"Photo_School");
 Book(g,new(4.45f,.907f,13.44f),new(.34f,.055f,.25f),-7);Book(g,new(4.43f,.969f,13.44f),new(.30f,.04f,.23f),4);
 CandleGroup(g,"Candles beside the safe",new(3.86f,.904f,13.43f),3,1.2f);
 var box=Group(g,"Box of family keepsakes");box.localPosition=new(4.15f,.012f,12.90f);Box(box,"Woven storage box",new(0,.13f,0),new(.46f,.26f,.32f),linen,.018f,true);Box(box,"Box lid",new(0,.27f,0),new(.48f,.028f,.34f),leather,.011f);Box(box,"Label on old box",new(0,.17f,-.165f),new(.16f,.067f,.006f),paper,.002f);
 }
 static Material CandleGlow(string name,Color colour,float strength){var path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",colour);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",colour*strength);m.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(m);return m;}
 static void CandleGroup(Transform p,string name,Vector3 pos,int count,float light){var group=Group(p,name);group.localPosition=pos;var wax=Mat("OldCreamBeeswax",new Color(.68f,.57f,.34f),2,.18f);var wick=Mat("CharredCandleWick",new Color(.035f,.025f,.018f),3,.05f);var orange=CandleGlow("CandleAmberFlame",new Color(1,.40f,.065f),3.5f);var core=CandleGlow("CandleGoldCore",new Color(1,.80f,.36f),5.5f);
 for(int i=0;i<count;i++){var c=Group(group,"Lit candle "+(i+1));c.localPosition=new((i-(count-1)*.5f)*.126f,0,i%2*.072f);float h=.14f+(i%3)*.055f,r=.038f+(i%2)*.007f;var k=new MeshKit();int metal=k.Mat("brass"),body=k.Mat("wax"),charred=k.Mat("wick");k.Lathe(new[]{new Vector2(0,0),new Vector2(r*1.55f,0),new Vector2(r*1.6f,.012f),new Vector2(r*1.25f,.022f),new Vector2(r*1.18f,.020f)},metal,28);k.Lathe(new[]{new Vector2(r,.024f),new Vector2(r,h),new Vector2(r*.83f,h+.005f),new Vector2(r*.48f,h-.009f),new Vector2(0,h-.012f)},body,28);k.Cylinder(new(0,h+.001f,0),.0022f,.02f,charred,10);for(int drip=0;drip<3;drip++){float a=drip*2.09f+i*.4f;using(k.At(new(Mathf.Cos(a)*r,h-.032f,Mathf.Sin(a)*r)))k.Lathe(new[]{new Vector2(.006f,-.03f),new Vector2(.007f,.02f),new Vector2(.004f,.035f)},body,10);}Mesh(c,"Candle holder wax and wick "+i,k,new[]{brass,wax,wick});
 var f=new MeshKit();int outer=f.Mat("amber"),inner=f.Mat("gold");using(f.At(new(0,h+.009f,0))){f.Lathe(new[]{new Vector2(0,0),new Vector2(.010f,.009f),new Vector2(.011f,.022f),new Vector2(.006f,.039f),new Vector2(0,.060f)},outer,20);f.Lathe(new[]{new Vector2(0,.001f),new Vector2(.005f,.007f),new Vector2(.005f,.018f),new Vector2(0,.030f)},inner,16);}var flame=Mesh(c,"Warm candle flame "+i,f,new[]{orange,core});flame.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
 }
 Lamp(group,"Candle light - "+name,new(0,.27f,.10f),light,4.4f);group.Find("Candle light - "+name).GetComponent<Light>().shadows=LightShadows.None;
 }
 static void Kitchen(Transform p){var g=Group(p,"Full inherited kitchen and supper corner");
 var fridge=Model(g,"kitchenFridge","Ivory refrigerator",new(-4.36f,.012f,5.52f),1.77f,180,.72f,true);
 foreach(var r in fridge.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name=="AgedBrass"?steel:enamel).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 var counter=Group(g,"Continuous kitchen counter");counter.localPosition=new(0,.012f,5.49f);
 foreach(float x in new[]{-3.44f,-2.54f,-1.64f}){
 Box(counter,"Cabinet carcass",new(x,x<-3?.36f:.43f,0),new(.884f,x<-3?.66f:.78f,.62f),wood,.016f,true);Box(counter,"Cabinet plinth",new(x,.075f,0),new(.85f,.10f,.56f),wood,.006f);
 if(x<-2){foreach(float sign in new[]{-1f,1f}){float xx=x+sign*.211f;Box(counter,"Framed cupboard door",new(xx,.45f,.325f),new(.407f,.68f,.035f),wood,.009f);Box(counter,"Recessed door panel",new(xx,.45f,.35f),new(.29f,.54f,.012f),Mat("FadedKitchenGreen",new Color(.29f,.34f,.26f),2,.22f),.012f);Box(counter,"Brass cabinet pull",new(xx-sign*.13f,.54f,.371f),new(.018f,.10f,.018f),brass,.007f);}}
 }
 // Cut the worktop into four pieces around the sink instead of covering its bowl.
 Box(counter,"Worktop left rim",new(-3.81f,.888f,0),new(.19f,.042f,.66f),enamel,.008f);
 Box(counter,"Worktop right stretch",new(-2.1725f,.888f,0),new(1.985f,.042f,.66f),enamel,.008f);
 foreach(float z in new[]{-.275f,.275f})Box(counter,"Worktop sink rim",new(-3.44f,.888f,z),new(.55f,.042f,.11f),enamel,.007f);
 var stainless=Mat("StainlessSinkInterior",new Color(.55f,.58f,.60f),6,.66f);stainless.SetFloat("_Metallic",1);EditorUtility.SetDirty(stainless);
 var sink=Group(counter,"Inset stainless steel sink");sink.localPosition=new(-3.44f,0,0);var k=new MeshKit();int bowl=k.Mat("enamel"),metal=k.Mat("metal");
 k.Box(new(0,.756f,0),new(.49f,.025f,.35f),bowl,.011f);
 foreach(float sign in new[]{-1f,1f}){using(k.At(new(sign*.249f,.828f,0),new(0,0,sign*-12)))k.Box(Vector3.zero,new(.016f,.15f,.37f),bowl,.004f);using(k.At(new(0,.828f,sign*.18f),new(sign*12,0,0)))k.Box(Vector3.zero,new(.51f,.15f,.016f),bowl,.004f);k.Box(new(sign*.273f,.907f,0),new(.019f,.016f,.418f),metal,.004f);k.Box(new(0,.907f,sign*.205f),new(.558f,.016f,.018f),metal,.004f);}
 k.Disc(new(0,.772f,0),.025f,metal,24);
 k.Sweep(new[]{new Vector3(0,.909f,-.26f),new Vector3(0,1.07f,-.26f),new Vector3(0,1.135f,-.24f),new Vector3(0,1.17f,-.18f),new Vector3(0,1.155f,-.11f),new Vector3(0,1.10f,-.08f)},new Vector2(.022f,.022f),metal);
 foreach(float x in new[]{-.15f,.15f}){k.Cylinder(new(x,.928f,-.26f),.022f,.038f,metal,16);k.Box(new(x,.955f,-.26f),new(.068f,.013f,.013f),metal,.004f);}
 Mesh(sink,"Stainless steel sink and faucet",k,new[]{stainless,steel});
 var stove=Group(counter,"Four burner stove and oven");stove.localPosition=new(-1.64f,0,0);var iron=Mat("StoveBlackIron",new Color(.07f,.075f,.065f),6,.30f);
 Box(stove,"Hob enamel plate",new(0,.92f,0),new(.76f,.035f,.56f),enamel,.018f);
 foreach(float x in new[]{-.21f,.21f})foreach(float z in new[]{-.14f,.14f}){var burners=new MeshKit();int m=burners.Mat("iron");burners.Cylinder(new(x,.947f,z),.10f,.012f,m,28);burners.Torus(new(x,.956f,z),.071f,.006f,m,28,8);burners.Torus(new(x,.956f,z),.045f,.005f,m,24,8);Mesh(stove,"Cast iron hob ring",burners,new[]{iron});}
 Box(stove,"Oven enamel front",new(0,.425f,.328f),new(.79f,.70f,.034f),enamel,.018f);Box(stove,"Dark oven glass",new(0,.395f,.350f),new(.59f,.37f,.015f),iron,.018f);Box(stove,"Oven handle",new(0,.65f,.39f),new(.57f,.029f,.035f),steel,.01f);
 for(int i=0;i<4;i++){var knob=Group(stove,"Stove control knob");knob.localPosition=new(-.255f+i*.17f,.772f,.36f);knob.localRotation=Quaternion.Euler(90,0,0);var knobs=new MeshKit();knobs.Cylinder(Vector3.zero,.033f,.036f,knobs.Mat("iron"),20);Mesh(knob,"Black oven dial",knobs,new[]{iron});}
 Box(g,"Cream tiled backsplash",new(-2.54f,1.22f,5.10f),new(2.75f,.54f,.025f),tiles,.004f);
 foreach(float x in new[]{-3.44f,-2.54f}){Box(g,"Upper kitchen cabinet",new(x,1.97f,5.30f),new(.86f,.68f,.37f),wood,.014f);foreach(float sign in new[]{-1f,1f}){Box(g,"Upper framed cupboard door",new(x+sign*.207f,1.97f,5.50f),new(.395f,.62f,.025f),wood,.008f);Box(g,"Upper door panel",new(x+sign*.207f,1.97f,5.52f),new(.28f,.48f,.009f),Mat("FadedKitchenGreen",new Color(.29f,.34f,.26f),2,.22f),.007f);Box(g,"Upper brass handle",new(x+sign*.073f,1.81f,5.535f),new(.015f,.077f,.016f),brass,.005f);}}
 foreach(float x in new[]{-3.44f,-2.54f}){Box(g,"Under-cabinet brass light",new(x,1.617f,5.49f),new(.64f,.022f,.045f),brass,.005f);Lamp(g,"Warm worktop light",new(x,1.57f,5.60f),.64f,2.8f);}
 Model(g,"kitchenCoffeeMachine","Old coffee machine",new(-2.61f,.94f,5.53f),.32f,180,.30f,false);Cup(g,new(-2.27f,.94f,5.62f));Bowl(g,new(-3.78f,.94f,5.67f),.085f);
 Box(g,"Linen drying towel",new(-3.62f,.92f,5.77f),new(.22f,.016f,.18f),linen,.005f);
 Model(g,"tableRound","Small supper table",new(2.55f,.012f,7.8f),.76f,0,1.0f,true);
 Model(g,"chair","Supper chair one",new(1.73f,.012f,7.8f),.95f,-90,0,true);Model(g,"chair","Supper chair two",new(3.39f,.012f,7.8f),.95f,90,0,true);
 Bowl(g,new(2.55f,.78f,7.8f),.17f);Cup(g,new(2.23f,.78f,7.69f));
 }
 static void Sconces(Transform p){var root=Group(p,"Warm wall sconces");var placements=new[]{(new Vector3(-4.87f,2.12f,2.75f),90f),(new Vector3(4.87f,2.12f,3.45f),-90f),(new Vector3(-1.60f,2.12f,5.09f),0f),(new Vector3(4.87f,2.12f,10.0f),-90f),(new Vector3(-.10f,2.12f,13.87f),180f)};int index=0;
 var glow=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/WarmSconceOpal.mat");if(!glow){glow=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(glow,Dir+"/WarmSconceOpal.mat");}glow.SetColor("_BaseColor",new Color(.78f,.67f,.45f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.66f,.29f)*1.25f);glow.SetFloat("_Smoothness",.27f);EditorUtility.SetDirty(glow);
 foreach(var place in placements){var g=Group(root,"Brass wall sconce "+(++index));g.localPosition=place.Item1;g.localRotation=Quaternion.Euler(0,place.Item2,0);var body=new MeshKit();int b=body.Mat("brass");body.Box(new(0,-.025f,0),new(.115f,.26f,.045f),b,.025f);body.Sweep(new[]{new Vector3(0,-.095f,.018f),new Vector3(0,-.13f,.13f),new Vector3(0,-.07f,.23f)},new Vector2(.023f,.023f),b);Mesh(g,"Brass sconce mounting and arm",body,new[]{brass});var opal=new MeshKit();using(opal.At(new(0,0,.23f)))opal.Lathe(new[]{new Vector2(.11f,-.025f),new Vector2(.095f,.095f),new Vector2(.069f,.18f)},opal.Mat("opal"),28);var glass=Mesh(g,"Opal sconce shade",opal,new[]{glow});glass.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
 Lamp(g,"Warm sconce light "+index,new(0,.045f,.28f),index==3?1.6f:1.35f,index==3?4.8f:5.1f);
 }
 }
 static void Curtains(Transform p){var g=Group(p,"Simple linen curtains and inherited shades");var windows=new[]{(new Vector3(-3.1f,1.65f,.115f),0f),(new Vector3(3.1f,1.65f,.115f),0f),(new Vector3(-4.87f,1.65f,8.5f),90f),(new Vector3(-4.87f,1.65f,11.5f),90f),(new Vector3(4.87f,1.65f,7.5f),-90f)};
 foreach(var window in windows){var frame=Group(g,"Window curtain pair");frame.localPosition=window.Item1;frame.localRotation=Quaternion.Euler(0,window.Item2,0);Box(frame,"Curtain rod",new(0,.79f,.035f),new(1.99f,.023f,.025f),brass,.008f);foreach(float sign in new[]{-1f,1f}){var panel=Group(frame,"Linen side panel");bool aboveHallChest=window.Item1.x>3&&window.Item1.z<.2f;panel.localPosition=new(sign*.79f,aboveHallChest?-.025f:-.055f,.08f);var kit=new MeshKit();int m=kit.Mat("linen"),start=kit.Sheet(new Vector2(.28f,aboveHallChest?1.45f:1.51f),18,18,m,m);kit.Deform(start,v=>new Vector3(v.x+sign*.025f*Mathf.Sin((v.y+.755f)*2),v.y,v.z+.028f*Mathf.Sin(v.x*85f)));Mesh(panel,"Pleated linen curtain",kit,new[]{linen});}}
 foreach(var pos in new[]{new Vector3(0,2.52f,2.5f),new Vector3(0,2.52f,9.5f)}){var shade=Group(g,"Old fabric pendant");shade.localPosition=pos;var k=new MeshKit();int cloth=k.Mat("shade"),metal=k.Mat("metal");k.Lathe(new[]{new Vector2(.29f,0),new Vector2(.21f,.23f),new Vector2(.20f,.23f),new Vector2(.28f,0)},cloth,32);k.Cylinder(new(0,.35f,0),.009f,.25f,metal,16);k.Cylinder(new(0,.48f,0),.055f,.025f,metal,20);Mesh(shade,"Inherited linen pendant shade",k,new[]{linen,brass}).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
 }
 static void Trim(Transform p,Transform geometry){var g=Group(p,"Old timber skirting");foreach(var r in geometry.GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("Wall")||r.name.StartsWith("Partition"))){var b=r.bounds;var c=p.InverseTransformPoint(b.center);if(b.min.y>.1f)continue;Vector3 size=b.size;size.y=.105f;size.x+=.025f;size.z+=.025f;Box(g,"Timber skirting",new(c.x,.065f,c.z),size,wood,.003f);}}
 static void Sofa(Transform p,Vector3 pos,float yaw){var g=Group(p,"Inherited two-seat settee");g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);Box(g,"Walnut base",new(0,.28f,0),new(2.18f,.18f,.79f),wood,.045f,true);foreach(float x in new[]{-.92f,.92f})foreach(float z in new[]{-.29f,.29f})Box(g,"Turned wood foot",new(x,.12f,z),new(.09f,.24f,.09f),wood,.018f);Box(g,"Soft back",new(0,.69f,.35f),new(2.12f,.67f,.19f),velvet,.075f);foreach(float x in new[]{-.52f,.52f})Box(g,"Seat cushion",new(x,.43f,-.03f),new(1.01f,.20f,.67f),velvet,.065f);foreach(float x in new[]{-1.08f,1.08f})Box(g,"Upholstered arm",new(x,.57f,0),new(.16f,.44f,.87f),velvet,.06f);}
 static void Shoes(Transform p,Vector3 pos,float yaw,bool boot){var g=Group(p,boot?"Pair of worn leather boots":"Pair of house shoes");g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);foreach(float x in new[]{-.12f,.12f}){Box(g,"Sole",new(x,.025f,0),new(.14f,.035f,.29f),leather,.016f);Box(g,"Leather upper",new(x,.084f,-.024f),new(.135f,.10f,.26f),leather,.04f);if(boot)Box(g,"Boot ankle",new(x,.19f,.068f),new(.12f,.19f,.11f),leather,.032f);for(int i=0;i<3;i++)Box(g,"Lace",new(x,.145f,.005f-i*.025f),new(.084f,.005f,.009f),linen,.002f);}}
 static void Pillow(Transform p,Vector3 pos,float yaw,Material material){var go=Box(p,"Soft cushion",pos,new(.42f,.36f,.14f),material,.064f);go.transform.localRotation=Quaternion.Euler(-12,yaw,0);}
 static void Rug(Transform p,string name,Vector3 pos,Vector3 size){Box(p,name,pos,size,rug,.004f);}
 static void Cup(Transform p,Vector3 pos){var g=Group(p,"Used tea cup");g.localPosition=pos;var k=new MeshKit();int m=k.Mat("cup"),tea=k.Mat("tea");k.Lathe(new[]{new Vector2(.04f,0),new Vector2(.043f,.015f),new Vector2(.048f,.10f),new Vector2(.045f,.108f),new Vector2(.039f,.10f),new Vector2(.036f,.02f)},m,24);k.Disc(new(0,.081f,0),.038f,tea);using(k.At(new(.052f,.055f,0),new(0,90,0)))k.Torus(Vector3.zero,.026f,.006f,m,18,8);k.Cylinder(new(0,-.003f,0),.077f,.009f,m,24);Mesh(g,"Tea cup and saucer",k,new[]{ceramic,Mat("Tea",new Color(.085f,.045f,.019f),5,.5f)});}
 static void Bowl(Transform p,Vector3 pos,float radius){var g=Group(p,"Stoneware bowl");g.localPosition=pos;var k=new MeshKit();int m=k.Mat("bowl");k.Lathe(new[]{new Vector2(radius*.3f,0),new Vector2(radius*.4f,.012f),new Vector2(radius,.08f),new Vector2(radius*.94f,.085f),new Vector2(radius*.36f,.018f)},m,24);Mesh(g,"Small glazed bowl",k,new[]{ceramic});}
 static void Book(Transform p,Vector3 pos,Vector3 size,float yaw){var g=Group(p,"Old book");g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);Box(g,"Cream pages",new(0,size.y*.5f,0),size,paper,.002f);foreach(float y in new[]{0,size.y})Box(g,"Cloth cover",new(0,y,0),new(size.x+.012f,.005f,size.z+.008f),rug,.002f);Box(g,"Book spine",new(-size.x*.5f,size.y*.5f,0),new(.008f,size.y,size.z),rug,.002f);}
 static void Frame(Transform p,string name,Vector3 pos,Vector2 size,float yaw,string texture){var g=Group(p,name);g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);Box(g,"Walnut frame",Vector3.zero,new(size.x+.07f,size.y+.07f,.035f),wood,.012f);Box(g,"Aged paper mount",new(0,0,.021f),new(size.x,size.y,.009f),paper,.001f);var q=GameObject.CreatePrimitive(PrimitiveType.Quad);q.name="Faded photograph";q.transform.SetParent(g,false);q.transform.localPosition=new(0,0,.027f);q.transform.localRotation=Quaternion.Euler(0,180,0);q.transform.localScale=new(size.x*.84f,size.y*.82f,1);Object.DestroyImmediate(q.GetComponent<Collider>());string path=Dir+"/"+texture+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Photos/"+texture+".png"));mat.SetColor("_BaseColor",new Color(.75f,.68f,.55f));mat.SetFloat("_Smoothness",.18f);Assign(q.GetComponent<Renderer>(),mat);}
 static void Lamp(Transform p,string name,Vector3 pos,float intensity,float range){var g=Group(p,name);g.localPosition=pos;var l=g.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.74f,.47f);l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;l.shadowBias=.04f;l.shadowNormalBias=.15f;}
 static Material Mat(string name,Color color,int kind,float smooth){var path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("HollowCreek/Inherited House Surfaces"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Kind",kind);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
 static void Assign(Renderer r,Material m){r.sharedMaterial=m;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 static void Retexture(Renderer r){r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;string n=m.name.ToLowerInvariant();if(n.Contains("wood"))return wood;if(n.Contains("carpet"))return r.name.ToLowerInvariant().Contains("rug")?rug:velvet;if(n.Contains("metal"))return brass;if(n=="plant")return green;return m;}).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 static Transform Group(Transform p,string name){var g=new GameObject(name).transform;g.SetParent(p,false);return g;}
 static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,float round=0,bool collision=false){var k=new MeshKit();k.Box(Vector3.zero,size,k.Mat("surface"),round,segments:3);var g=Mesh(p,name,k,new[]{mat});g.transform.localPosition=pos;if(collision){var c=g.AddComponent<BoxCollider>();c.size=size;}return g;}
 static GameObject Mesh(Transform p,string name,MeshKit k,Material[] materials){string filename=new string(name.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());var built=k.Build(name);string path=Dir+"/"+filename+".asset"; // Geometry variants need separate assets.
 path=Dir+"/"+filename+"_"+built.vertexCount+"_"+Mathf.Abs(StableHash(string.Join(";",built.vertices.Select(v=>v.ToString("F4")))))+".asset";
 var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)Object.DestroyImmediate(built);else{mesh=built;AssetDatabase.CreateAsset(mesh,path);}var go=new GameObject(name);go.transform.SetParent(p,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;return go;}
 static int StableHash(string s){unchecked{int hash=17;foreach(char c in s)hash=hash*31+c;return hash;}}
 static GameObject Model(Transform p,string asset,string name,Vector3 pos,float height,float yaw,float width,bool collision){var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Furniture/"+asset+".fbx");var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.SetParent(p,false);go.transform.localScale=Vector3.one;go.transform.localPosition=Vector3.zero;var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float sy=height/b.size.y,sx=width>0?width/Mathf.Max(b.size.x,b.size.z):sy;go.transform.localScale=new(sx,sy,sx);go.transform.localRotation=Quaternion.Euler(0,yaw,0);b=rs[0].bounds;foreach(var r in rs){b.Encapsulate(r.bounds);Retexture(r);}var bottom=new Vector3(b.center.x,b.min.y,b.center.z);go.transform.position+=p.TransformPoint(pos)-bottom;
 if(collision){var box=go.AddComponent<BoxCollider>();var local=new Bounds();bool first=true;foreach(var f in go.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices){var pt=go.transform.InverseTransformPoint(f.transform.TransformPoint(v));if(first){local=new Bounds(pt,Vector3.zero);first=false;}else local.Encapsulate(pt);}box.center=local.center;box.size=local.size;}
 PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);return go;}
 }
}















