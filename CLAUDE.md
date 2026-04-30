# Code Style

## Access Modifiers
Always declare access modifiers explicitly on all fields, properties, and methods — never rely on implicit `private`.

```csharp
// correct
private int _count;
private void Start() { }
public bool IsPlaying { get; private set; }

// wrong
int _count;
void Start() { }
bool IsPlaying { get; private set; }
```

## No Null Checks for Required References

Do not guard against null for `[SerializeField]` fields that must be wired up in the Inspector, or for singletons (e.g. `GameManager.Instance`) that are guaranteed to exist in the scene. If they are missing, it is a setup error — let Unity throw so it is caught immediately, not silently swallowed.

```csharp
// correct
timerText.text = $"{minutes:00}:{seconds:00}";
GameManager.Instance.Win();

// wrong
if (timerText != null) timerText.text = $"{minutes:00}:{seconds:00}";
if (GameManager.Instance != null) GameManager.Instance.Win();
```