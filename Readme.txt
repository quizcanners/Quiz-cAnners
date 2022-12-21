My Utilities to easily make CustomEditors. This are just wrappers on top of Unity's Editor existing functionality. Absolutely nothing special or new.

Latest Version is always at:
https://github.com/quizcanners/Quiz-cAnners

Use IPEGI interface to provie Inspect() fucntion (Look at some examples).

HOW TO USE IN EDITOR TIME:

	  Anywhere (I usually put it in the same .cs file, right under the class):
	  [PEGI_Inspector_Override(typeof(YourClass))] internal class YourClassDrawer : PEGI_Inspector_Override { }


HOW TO USE IT IN PLAYTIME:

    In MonoBehaviour that implements IPEGI, add the following code:
	pegi.GameView.Window window = new();
	public void OnGUI() =>	window.Render(this, "YOUR HEADER TITLE");
	
