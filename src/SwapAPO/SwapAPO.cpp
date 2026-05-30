#include "SwapAPO.h"
#include <cstring>

SwapAPOModule _AtlModule;

// Registration properties
const CRegAPOProperties<1> SwapAPO::sm_RegProperties(
    CLSID_SwapAPO,
    L"SwapAPO", L"",
    1, 0,
    __uuidof(IAudioProcessingObjectRT),
    static_cast<APO_FLAG>(APO_FLAG_INPLACE | APO_FLAG_SAMPLESPERFRAME_MUST_MATCH |
        APO_FLAG_FRAMESPERSECOND_MUST_MATCH | APO_FLAG_BITSPERSAMPLE_MUST_MATCH));

SwapAPO::SwapAPO()
    : CBaseAudioProcessingObject(sm_RegProperties)
{
}

STDMETHODIMP_(void) SwapAPO::APOProcess(
    UINT32 u32NumInputConnections,
    APO_CONNECTION_PROPERTY** ppInputConnections,
    UINT32 u32NumOutputConnections,
    APO_CONNECTION_PROPERTY** ppOutputConnections)
{
    APO_CONNECTION_PROPERTY* pInput = ppInputConnections[0];
    APO_CONNECTION_PROPERTY* pOutput = ppOutputConnections[0];

    if (pInput->u32BufferFlags == BUFFER_SILENT) {
        pOutput->u32BufferFlags = BUFFER_SILENT;
        pOutput->u32ValidFrameCount = pInput->u32ValidFrameCount;
        return;
    }

    float* pIn  = reinterpret_cast<float*>(pInput->pBuffer);
    float* pOut = reinterpret_cast<float*>(pOutput->pBuffer);
    UINT32 frames = pInput->u32ValidFrameCount;
    UINT32 ch = m_u32SamplesPerFrame > 0 ? m_u32SamplesPerFrame : 2;

    if (ch >= 2) {
        for (UINT32 i = 0; i < frames; i++) {
            UINT32 off = i * ch;
            float tmpL = pIn[off + 0];
            pOut[off + 0] = pIn[off + 1];
            pOut[off + 1] = tmpL;
            for (UINT32 c = 2; c < ch; c++)
                pOut[off + c] = pIn[off + c];
        }
    } else if (pOut != pIn) {
        memcpy(pOut, pIn, frames * ch * sizeof(float));
    }

    pOutput->u32BufferFlags = BUFFER_VALID;
    pOutput->u32ValidFrameCount = frames;
}

// DLL exports via ATL
STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}

STDAPI DllCanUnloadNow()
{
    return _AtlModule.DllCanUnloadNow();
}

BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    return _AtlModule.DllMain(dwReason, lpReserved);
}
