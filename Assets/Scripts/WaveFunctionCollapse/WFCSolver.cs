using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;


public class WFCSolver
{
    private class Node
    {
        public HashSet<WFCPositionedModule> m_posibleModules = new HashSet<WFCPositionedModule>();
        public Dictionary<string, int>[] m_remainingConnectors = new Dictionary<string, int>[6];
        private Vector3Int m_position;
        private WFCSolver.Node[,,] m_map;
        private Vector3Int m_mapSize;

        public Node(Vector3Int position, WFCSolver.Node[,,] map, Vector3Int mapSize)
        {
            m_position = position;
            m_map = map;
            m_mapSize = mapSize;
            for (int i = 0; i < 6; i++)
            {
                m_remainingConnectors[i] = new Dictionary<string, int>();
            }
        }

        private void AddConnector(int dir, string connector)
        {
            if (m_remainingConnectors[dir].ContainsKey(connector))
            {
                m_remainingConnectors[dir][connector] += 1;
            }
            else
            {
                m_remainingConnectors[dir].Add(connector, 1);
            }
        }

        public void SetModule(WFCModule module, int rotation)
        {
            WFCPositionedModule pModule = new WFCPositionedModule(module, m_position, rotation);

            if (!pModule.IsPossible(m_mapSize)) { throw new System.ArgumentException("Module cannot be placed at the requested location"); }

            List<WFCPositionedModule> currModules = m_posibleModules.ToList();
            AddModule(pModule);
            foreach (WFCPositionedModule other in currModules)
            {
                RemoveModule(other);
            }
        }

        public void ApplyNegativeConstraint(int dir, string connector)
        {
            foreach (WFCPositionedModule pModule in m_posibleModules.ToList())
            {
                Vector3Int modulePosition = pModule.GetPosition();
                Vector3Int relPos = m_position - modulePosition;
                if (pModule.GetConnector(relPos, dir) == connector)
                {
                    m_map[modulePosition.x, modulePosition.y, modulePosition.z].RemoveModule(pModule);
                }
            }
        }

        public void ApplyPositiveConstraint(int dir, string connector)
        {
            foreach (WFCPositionedModule pModule in m_posibleModules.ToList())
            {
                Vector3Int modulePosition = pModule.GetPosition();
                Vector3Int relPos = m_position - modulePosition;
                if (pModule.GetConnector(relPos, dir) != connector)
                {
                    m_map[modulePosition.x, modulePosition.y, modulePosition.z].RemoveModule(pModule);
                }
            }
        }

        private void RemoveConnector(int dir, string connector)
        {
            if (m_remainingConnectors[dir][connector] == 1)
            {
                Vector3Int adja = m_position + WFCUtils.dirOffset[dir];
                if (WFCUtils.IsValidPosition(adja, m_mapSize))
                {
                    m_map[adja.x, adja.y, adja.z].ApplyNegativeConstraint((dir + 3) % 6, WFCUtils.FlipConnector(connector));
                }
                m_remainingConnectors[dir].Remove(connector);
            }
            else
            {
                m_remainingConnectors[dir][connector] -= 1;
            }
        }

        private void AddModule(WFCPositionedModule pModule)
        {
            foreach (Vector3Int relPos in pModule.GetSubModulesPositions())
            {
                m_map[m_position.x + relPos.x, m_position.y + relPos.y, m_position.z + relPos.z].m_posibleModules.Add(pModule);

                for (int dir = 0; dir < 6; dir++)
                {
                    string connector = pModule.GetConnector(relPos, dir);
                    if (connector != null)
                    {
                        m_map[m_position.x + relPos.x, m_position.y + relPos.y, m_position.z + relPos.z].AddConnector(dir, connector);
                    }
                }
            }
        }

        public void AddModules(List<WFCModule> moduleList)
        {
            foreach (WFCModule module in moduleList)
            {
                if (!module.IsGroundOnly() || m_position.y == 0)
                {
                    WFCPositionedModule pModule = new WFCPositionedModule(module, m_position);
                    if (pModule.IsPossible(m_mapSize))
                    {
                        AddModule(pModule);
                    }

                    for (int rot = 1; rot < 4; rot++)
                    {
                        pModule = new WFCPositionedModule(pModule, 1);
                        if (pModule.IsPossible(m_mapSize))
                        {
                            AddModule(pModule);
                        }
                    }

                    if (module.IsFlippable())
                    {
                        pModule = new WFCPositionedModule(module, m_position, 0, true);
                        if (pModule.IsPossible(m_mapSize))
                        {
                            AddModule(pModule);
                        }

                        for (int rot = 1; rot < 4; rot++)
                        {
                            pModule = new WFCPositionedModule(pModule, 1);
                            if (pModule.IsPossible(m_mapSize))
                            {
                                AddModule(pModule);
                            }
                        }
                    }
                }
            }
        }

        private void RemoveModule(WFCPositionedModule pModule)
        {
            foreach (Vector3Int relPos in pModule.GetSubModulesPositions())
            {
                if (m_map[m_position.x + relPos.x, m_position.y + relPos.y, m_position.z + relPos.z].m_posibleModules.Remove(pModule))
                {
                    for (int dir = 0; dir < 6; dir++)
                    {
                        string connector = pModule.GetConnector(relPos, dir);
                        if (connector != null)
                        {
                            m_map[m_position.x + relPos.x, m_position.y + relPos.y, m_position.z + relPos.z].RemoveConnector(dir, connector);
                        }
                    }
                }
            }
        }

        public void Collapse()
        {
            if (m_posibleModules.Count < 2) { return; }

            int selected = WFCUtils.randGen.Next(0, m_posibleModules.Count);
            int i = 0;
            foreach (WFCPositionedModule pModule in m_posibleModules.ToList())
            {
                if (i != selected)
                {
                    Vector3Int modulePosition = pModule.GetPosition();
                    m_map[modulePosition.x, modulePosition.y, modulePosition.z].RemoveModule(pModule);
                }
                i += 1;
            }
        }

        public void OnDrawGizmos()
        {
            foreach (WFCPositionedModule pModule in m_posibleModules)
            {
                if (pModule.GetPosition() == m_position)
                {
                    pModule.OnDrawGizmos();
                }
            }
        }
    }

    private Vector3Int m_size;
    private WFCSolver.Node[,,] m_map;
    private List<WFCModule> m_moduleList;
    private HashSet<Vector3Int> m_explored = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> m_toExplore = new HashSet<Vector3Int>();

    public WFCSolver(Vector3Int size, List<WFCModule> moduleList)
    {
        m_size = size;
        m_moduleList = moduleList;
        this.InitMap();
    }

    private void InitMap()
    {
        m_map = new WFCSolver.Node[m_size.x, m_size.y, m_size.z];
        for (int x = 0; x < m_size.x; x++)
        {
            for (int y = 0; y < m_size.y; y++)
            {
                for (int z = 0; z < m_size.z; z++)
                {
                    m_map[x, y, z] = new WFCSolver.Node(new Vector3Int(x, y, z), m_map, m_size);
                }
            }
        }

        for (int x = 0; x < m_size.x; x++)
        {
            for (int y = 0; y < m_size.y; y++)
            {
                for (int z = 0; z < m_size.z; z++)
                {
                    m_map[x, y, z].AddModules(m_moduleList);
                }
            }
        }
    }

    public void ApplyBorderConstraints(string bottom, string top, string side, string ground)
    {
        for (int x = 0; x < m_size.x; x++)
        {
            for (int z = 0; z < m_size.z; z++)
            {
                m_map[x, 0, z].ApplyPositiveConstraint(WFCUtils.NEGY, bottom);
                m_map[x, m_size.y - 1, z].ApplyPositiveConstraint(WFCUtils.POSY, top);
            }
        }

        for (int x = 0; x < m_size.x; x++)
        {
            m_map[x, 0, 0].ApplyPositiveConstraint(WFCUtils.NEGZ, ground);
            m_map[x, 0, m_size.z - 1].ApplyPositiveConstraint(WFCUtils.POSZ, ground);
            for (int y = 1; y < m_size.y; y++)
            {
                m_map[x, y, 0].ApplyPositiveConstraint(WFCUtils.NEGZ, side);
                m_map[x, y, m_size.z - 1].ApplyPositiveConstraint(WFCUtils.POSZ, side);
            }
        }

        for (int z = 0; z < m_size.z; z++)
        {
            m_map[0, 0, z].ApplyPositiveConstraint(WFCUtils.NEGX, ground);
            m_map[m_size.x - 1, 0, z].ApplyPositiveConstraint(WFCUtils.POSX, ground);
            for (int y = 1; y < m_size.y; y++)
            {
                m_map[0, y, z].ApplyPositiveConstraint(WFCUtils.NEGX, side);
                m_map[m_size.x - 1, y, z].ApplyPositiveConstraint(WFCUtils.POSX, side);
            }
        }
    }

    public void SetModule(WFCModule module, Vector3Int pos, int rotation)
    {
        m_map[pos.x, pos.y, pos.z].SetModule(module, rotation);
        m_explored.Add(pos);
        foreach (Vector3Int offset in WFCUtils.dirOffset)
        {
            Vector3Int adja = pos + offset;
            if (WFCUtils.IsValidPosition(adja, m_size) && !m_explored.Contains(adja))
            {
                m_toExplore.Add(adja);
            }
        }
    }

    public void OnDrawGizmos()
    {
        for (int x = 0; x < m_size.x; x++)
        {
            for (int y = 0; y < m_size.y; y++)
            {
                for (int z = 0; z < m_size.z; z++)
                {
                    m_map[x, y, z].OnDrawGizmos();
                }
            }
        }
    }

    private void Step()
    {
        Vector3Int pos = m_toExplore.ElementAt(WFCUtils.randGen.Next(m_toExplore.Count));
        m_toExplore.Remove(pos);
        m_explored.Add(pos);
        m_map[pos.x, pos.y, pos.z].Collapse();
        foreach (Vector3Int offset in WFCUtils.dirOffset)
        {
            Vector3Int adja = pos + offset;
            if (WFCUtils.IsValidPosition(adja, m_size) && !m_explored.Contains(adja))
            {
                m_toExplore.Add(adja);
            }
        }
    }

    public void Solve()
    {
        if (m_toExplore.Count == 0)
        {
            Vector3Int randPos = new Vector3Int(WFCUtils.randGen.Next(m_size.x), WFCUtils.randGen.Next(m_size.y), WFCUtils.randGen.Next(m_size.z));
            m_toExplore.Add(randPos);
        }

        while (m_toExplore.Count > 0)
        {
            this.Step();
        }
    }

    public List<WFCPositionedModule> GetMap()
    {
        List<WFCPositionedModule> pModules = new List<WFCPositionedModule>();
        for (int x = 0; x < m_size.x; x++)
        {
            for (int y = 0; y < m_size.y; y++)
            {
                for (int z = 0; z < m_size.z; z++)
                {
                    WFCPositionedModule module = m_map[x, y, z].m_posibleModules.ElementAt(0);
                    if (!module.IsEmpty() && module.GetPosition() == new Vector3Int(x, y, z)) { pModules.Add(module); }
                }
            }
        }
        return pModules;
    }
}