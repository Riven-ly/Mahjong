import io

path = r'D:\AA_Mahjong\Git_Mahjong\Assets\Resources\UI\Panel\GameWinPanel.prefab'

BLOCK = '''--- !u!1001 &9000000000000000001
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 4479471338473357113}
    m_Modifications:
    - target: {fileID: 7395247312038452672, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_Name
      value: Caidai
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalScale.x
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalScale.y
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalScale.z
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_AnchoredPosition.x
      value: 3
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_AnchoredPosition.y
      value: -952
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalRotation.w
      value: -0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 270
      objectReference: {fileID: 0}
    - target: {fileID: 7395247312053382172, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
      propertyPath: InitialModule.startSpeed.scalar
      value: 34
      objectReference: {fileID: 0}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
--- !u!224 &9000000000000000002 stripped
RectTransform:
  m_CorrespondingSourceObject: {fileID: 7395247312038173656, guid: 5bd8d0c11e799bc4e8ad2d9655a55a40, type: 3}
  m_PrefabInstance: {fileID: 9000000000000000001}
  m_PrefabAsset: {fileID: 0}
'''

t = io.open(path, encoding='utf-8', newline='').read()
if '9000000000000000001' in t:
    print('ALREADY PRESENT - abort')
else:
    if not t.endswith('\n'):
        t += '\n'
    t += BLOCK
    io.open(path, 'w', encoding='utf-8', newline='').write(t)
    print('APPENDED ok, new length', len(t))
