#pragma once

#include <atlbase.h>
#include <atlcom.h>
#include <audioenginebaseapo.h>
#include <baseaudioprocessingobject.h>
#include <initguid.h>

// {1B5C2483-B741-4C18-9B0E-8B07FF3CA0F2}
DEFINE_GUID(CLSID_SwapAPO,
    0x1b5c2483, 0xb741, 0x4c18, 0x9b, 0x0e, 0x8b, 0x07, 0xff, 0x3c, 0xa0, 0xf2);

class ATL_NO_VTABLE SwapAPO
    : public CComObjectRootEx<CComMultiThreadModel>
    , public CComCoClass<SwapAPO, &CLSID_SwapAPO>
    , public CBaseAudioProcessingObject
    , public IAudioSystemEffects
{
public:
    SwapAPO();

    DECLARE_NO_REGISTRY()
    DECLARE_POLY_AGGREGATABLE(SwapAPO)

    BEGIN_COM_MAP(SwapAPO)
        COM_INTERFACE_ENTRY(IAudioProcessingObject)
        COM_INTERFACE_ENTRY(IAudioProcessingObjectRT)
        COM_INTERFACE_ENTRY(IAudioProcessingObjectConfiguration)
        COM_INTERFACE_ENTRY(IAudioSystemEffects)
    END_COM_MAP()

    // IAudioProcessingObjectRT (must override — pure virtual)
    STDMETHOD_(void, APOProcess)(
        UINT32 u32NumInputConnections,
        APO_CONNECTION_PROPERTY** ppInputConnections,
        UINT32 u32NumOutputConnections,
        APO_CONNECTION_PROPERTY** ppOutputConnections) override;

    static const CRegAPOProperties<1> sm_RegProperties;
};

// ATL module
class SwapAPOModule : public CAtlDllModuleT<SwapAPOModule> {};
extern SwapAPOModule _AtlModule;

OBJECT_ENTRY_AUTO(CLSID_SwapAPO, SwapAPO)
