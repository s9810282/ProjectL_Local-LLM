using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Framework.DataBase
{
    // --- �������̽� ���� ---
    public interface IDatabaseCatalog { }
    public interface IDatabaseGroup { }
    public interface IDatabaseTable { }
    public interface IDatabaseItem { }

    // �ھ� ������ ���Ե� ���� C# �������� ��Ŀ �������̽�
    public interface IData { }

    public interface IExcelData : IDatabaseItem, IData { }

    public interface IDataProvider<out TData>
    {
        IEnumerable<TData> GetAllData();
    }

    // --- 1. �ֻ��� īŻ�α� ---
    [CreateAssetMenu(fileName = "DatabaseCatalogSO", menuName = "Scriptable Objects/DatabaseCatalogSO")]
    public class DatabaseCatalogSO : ScriptableObject, IDatabaseCatalog, IDataProvider<IDatabaseGroup>, IData
    {
        [SerializeField] private List<DatabaseGroup> databaseGroups = new List<DatabaseGroup>();

        public IEnumerable<IDatabaseGroup> GetAllData()
        {
            return databaseGroups;
        }
    }

    // --- 2. �׷� ---
    public abstract class DatabaseGroup : ScriptableObject, IDatabaseGroup, IDataProvider<IDatabaseTable>, IData
    {
        [SerializeField] private List<DatabaseTable> databaseTables = new List<DatabaseTable>();

        public IEnumerable<IDatabaseTable> GetAllData()
        {
            return databaseTables;
        }
    }

    // --- 3. ���̺� ---
    public abstract class DatabaseTable : ScriptableObject, IDatabaseTable, IDataProvider<IDatabaseItem>, IData
    {
        [SerializeField] private List<DatabaseItem> databaseItems = new List<DatabaseItem>();

        public virtual IEnumerable<IDatabaseItem> GetAllData()
        {
            return databaseItems;
        }
    }

    // --- 4. ������ (���� ���) ---
    public abstract class DatabaseItem : ScriptableObject, IDatabaseItem, IData
    {
        public abstract IData GetData();
    }
}