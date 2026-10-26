"""Resume the official Unity installer with verified HTTP ranges and catalog checksum."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from urllib.request import Request, urlopen
import hashlib
import shutil
import time

URL='https://download.unity3d.com/download_unity/b22767f04f04/Windows64EditorInstaller/UnitySetup64-6000.6.7f1.exe'
TOTAL=3854250800
MD5='75688028a67f622bb9c50f6d2d0bd744'
DEST=Path('E:/Unity/Downloads/UnitySetup64-6000.6.7f1.exe')
PREFIX=DEST.with_suffix('.exe.prefix')


def digest(path):
    md5=hashlib.md5()
    with path.open('rb') as file:
        for block in iter(lambda:file.read(4*1024*1024),b''): md5.update(block)
    return md5.hexdigest()


def main():
    if DEST.exists() and DEST.stat().st_size==TOTAL:
        assert digest(DEST)==MD5
        print('Official catalog MD5 verified:',DEST,flush=True)
        return
    if not PREFIX.exists():
        if DEST.exists(): DEST.rename(PREFIX)
        else: PREFIX.write_bytes(b'')
    prefix_size=PREFIX.stat().st_size
    assert prefix_size<TOTAL
    size=(TOTAL-prefix_size+7)//8
    jobs=[]
    for index in range(8):
        start=prefix_size+index*size
        if start>=TOTAL: break
        jobs.append((start,min(TOTAL-1,start+size-1),DEST.with_suffix(f'.exe.part{index:02}')))

    def fetch(job):
        first,last,path=job
        for attempt in range(4):
            have=path.stat().st_size if path.exists() else 0
            assert have<=last-first+1
            if have==last-first+1: return
            start=first+have
            try:
                request=Request(URL,headers={'Range':f'bytes={start}-{last}'})
                with urlopen(request,timeout=60) as response:
                    assert response.status==206
                    assert response.headers['Content-Range']==f'bytes {start}-{last}/{TOTAL}'
                    with path.open('ab') as file:
                        for block in iter(lambda:response.read(256*1024),b''): file.write(block)
                assert path.stat().st_size==last-first+1
                return
            except Exception:
                if attempt==3: raise
                time.sleep(2)

    print('Preserved contiguous prefix:',prefix_size,'bytes; resuming remaining ranges.',flush=True)
    with ThreadPoolExecutor(max_workers=8) as pool:
        tasks=[pool.submit(fetch,job) for job in jobs]
        while not all(task.done() for task in tasks):
            done=prefix_size+sum(path.stat().st_size if path.exists() else 0 for _,_,path in jobs)
            print(f'Download {done/TOTAL:.1%} ({done:,}/{TOTAL:,} bytes)',flush=True)
            time.sleep(15)
        for task in tasks: task.result()
    assembly=DEST.with_suffix('.exe.assembling')
    with assembly.open('wb') as file:
        for path in [PREFIX]+[job[2] for job in jobs]:
            with path.open('rb') as source: shutil.copyfileobj(source,file,4*1024*1024)
    assert assembly.stat().st_size==TOTAL
    assert digest(assembly)==MD5,'Installer checksum differs from Unity release catalog.'
    assembly.rename(DEST)
    print('Complete; official catalog MD5 verified:',DEST,flush=True)


if __name__=='__main__': main()
