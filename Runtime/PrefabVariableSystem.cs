using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Globalization;

[ExecuteInEditMode]
public abstract class PrefabVariableSystem : MonoBehaviour
{
    public List<PrefabVariable> PrefabVariables = null;

    [SerializeField] public string gUID;
    void GenerateGUID()
    {
        if (gUID != "") return;
        gUID = Guid.NewGuid().ToString();
    }
    void Start()
    {
        GenerateGUID();
        if (!Application.isPlaying) return;
        DeclareVariables();
        OnStart();
    }

    public virtual void OnStart() { }
    public virtual void OnUpdate() { }
    public virtual void VariableUpdate() { }

    [Serializable]
    public class PrefabVariable
    {
        public string Name;
        public VariableType Type;
        [Header("Value")]
        public bool isList;
        public List<string> ValueList;
        [NonSerialized] public PrefabVariableSystem System;

        public bool SetVariable<T>(T value)
        {
            if (isList)
            {
                ValueList = new List<string> { value.ToString() };
            }
            else
            {
                if (ValueList == null)
                    ValueList = new List<string>();

                if (ValueList.Count == 0)
                    ValueList.Add(value.ToString());
                else
                    ValueList[0] = value.ToString();
            }
            System.VariableUpdate();
            return true;
        }

        public bool SetVariable<T>(List<T> valueList)
        {
            ValueList = valueList.ConvertAll(v => v.ToString());
            System.VariableUpdate();
            return true;
        }

        public T GetVariable<T>()
        {
            try
            {
                if (!isList)
                {
                    return ConvertValue<T>(ValueList[0], Type);
                }
                else
                {
                    Debug.LogError($"Variable '{Name}' is a list. Use GetVariableList instead.");
                    return default;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting variable '{Name}': {ex.Message}");
                return default;
            }
        }

        public List<T> GetVariableList<T>()
        {
            try
            {
                var result = new List<T>();
                foreach (var value in ValueList)
                {
                    result.Add(ConvertValue<T>(value,Type));
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting variable '{Name}': {ex.Message}");
                return new List<T>();
            }
        }

    }

    bool lastSelection;
    void Update()
    {
        GenerateGUID();
        bool selection = Selection.Contains(gameObject);
        if (selection && !lastSelection && Application.isEditor)
            DeclareVariables();
        lastSelection = selection;

        if (Application.isPlaying)
            OnUpdate();
    }

    public enum VariableType
    {
        Int,
        Float,
        String,
        Bool,
        Vector3
    }

    public virtual void DeclareVariables() { PrefabVariables = new(); }

    public PrefabVariable GetVariableByName(string name)
    {
        foreach(PrefabVariable prefabVariable in PrefabVariables)
        {
            if (prefabVariable.Name != name) continue;
            return prefabVariable;
        }
        return null;
    }

    public bool SetVariable<T>(string variableName, T value)
    {
        var variable = PrefabVariables.Find(v => v.Name == variableName);
        if (variable != null)
        {
            return variable.SetVariable(value);
        }
        else
        {
            return false;
        }
    }

    public bool SetVariable<T>(string variableName, List<T> valueList)
    {
        var variable = PrefabVariables.Find(v => v.Name == variableName);
        if (variable != null && variable.isList)
        {
            return variable.SetVariable(valueList);
        }
        else
        {
            Debug.LogError($"Variable '{variableName}' not found or is not a list.");
            return false;
        }
    }

    public T GetVariable<T>(string variableName)
    {
        var variable = PrefabVariables.Find(v => v.Name == variableName);
        if (variable != null)
        {
            return variable.GetVariable<T>();
        }
        else
        {
            Debug.LogError($"Variable '{variableName}' not found.");
            return default;
        }
    }

    public List<T> GetVariableList<T>(string variableName)
    {
        var variable = PrefabVariables.Find(v => v.Name == variableName);
        if (variable != null && variable.isList)
        {
            return variable.GetVariableList<T>();
        }
        else
        {
            Debug.LogError($"Variable '{variableName}' not found or is not a list.");
            return new List<T>();
        }
    }

    private static T ConvertValue<T>(string value, VariableType type)
    {
        Type targetType = typeof(T);
        switch (type)
        {
            case VariableType.Int:
                return (T)(object)int.Parse(value);
            case VariableType.Float:
                return (T)(object)float.Parse(value);
            case VariableType.String:
                return (T)(object)value;
            case VariableType.Bool:
                return (T)(object)bool.Parse(value);
            case VariableType.Vector3:
                return (T)(object)V3Parse(value);
            default:
                throw new NotSupportedException($"Type '{type}' is not supported.");
        }
    }

    private VariableType GetVariableType(Type type)
    {
        if (type == typeof(int))
            return VariableType.Int;
        if (type == typeof(float))
            return VariableType.Float;
        if (type == typeof(string))
            return VariableType.String;
        if (type == typeof(bool))
            return VariableType.Bool;
        if (type == typeof(Vector3))
            return VariableType.Vector3;

        throw new NotSupportedException($"Type '{type.Name}' is not supported.");
    }

    public bool AddVariable<T>(string name, VariableType type, T value, List<T> valueList = null)
    {
        if (PrefabVariables.Exists(v => v.Name == name))
        {
            return false;
        }

        var ValueList = new List<string>();

        if (valueList != null)
        {
            foreach (var item in valueList)
            {
                ValueList.Add(value.ToString());
            }
        }
        else
            ValueList.Add(value.ToString());

        PrefabVariables.Add(new PrefabVariable
        {
            Name = name,
            Type = type,
            isList = valueList != null,
            ValueList = ValueList,
            System = this
        });
        VariableUpdate();
        return true;
    }

    public bool RemoveVariable(string name)
    {
        var variable = PrefabVariables.Find(v => v.Name == name);
        if (variable != null)
        {
            PrefabVariables.Remove(variable);
            VariableUpdate();
            return true;
        }
        else
        {
            Debug.LogError($"Variable '{name}' not found.");
            return false;
        }
    }

    public virtual void Saved() {}
    public virtual void Loaded() {}

    public SavedPrefabVariableSystem Save()
    {
        Saved();
        SavedPrefabVariableSystem saved = new SavedPrefabVariableSystem
        {
            gUID = gUID,
            prefabVariables = PrefabVariables
        };

        return saved;
    }

    public void Load (SavedPrefabVariableSystem saved)
    {
        VariableUpdate();
        foreach (PrefabVariable prefabVariable in saved.prefabVariables)
        {
            GetVariableByName(prefabVariable.Name).ValueList = prefabVariable.ValueList;
        }
        Loaded();
    }

    [Serializable]
    public class SavedPrefabVariableSystem
    {
        public string gUID;
        public List<PrefabVariable> prefabVariables;
    }

    public static Vector3 V3Parse(string vectorString)
    {
        vectorString = vectorString.Trim('(', ')');
        string[] values = vectorString.Split(',');

        if (values.Length != 3)
        {
            throw new System.FormatException("Input string is not in the correct format.");
        }

        float x = float.Parse(values[0], CultureInfo.InvariantCulture);
        float y = float.Parse(values[1], CultureInfo.InvariantCulture);
        float z = float.Parse(values[2], CultureInfo.InvariantCulture);

        return new Vector3(x, y, z);
    }
}

