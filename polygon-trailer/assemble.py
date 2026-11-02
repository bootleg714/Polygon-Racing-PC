import subprocess
names=['COASTAL EXPRESSWAY','DOWNTOWN CIRCUIT','RIVIERA HARBOUR','SAKURA HILLS','NORDWALD RING','NEON MERIDIAN','ION RIFT','AURORA FORGE','STARFALL PORT']
cmd=['ffmpeg','-y','-hide_banner','-loglevel','warning']
for i in range(9):cmd += ['-framerate','15','-i',f'frames/{i:02d}/%03d.jpg']
cmd += ['-i','music.wav']
font='trailer-font.ttf'
filters=[]
for i,name in enumerate(names):
 chain=f'[{i}:v]fps=30,eq=contrast=1.06:saturation=1.12,drawbox=x=0:y=ih-116:w=iw:h=116:color=black@0.45:t=fill,drawtext=fontfile={font}:text=\'{name}\':fontcolor=white:fontsize=36:x=70:y=h-88,drawtext=fontfile={font}:text=\'POLYGON RACING PC\':fontcolor=0x66e7ff:fontsize=18:x=72:y=h-40,fade=t=in:st=0:d=0.18,fade=t=out:st=2.82:d=0.18'
 if i==0: chain+=f',drawtext=fontfile={font}:text=\'NINE CIRCUITS. ONE DRIVE.\':fontcolor=white:fontsize=36:x=(w-text_w)/2:y=95:enable=\'lt(t,2.6)\''
 filters.append(chain+f'[v{i}]')
filters.append('color=c=0x080d16:s=1280x720:r=30:d=3,drawtext=fontfile='+font+':text=\'POLYGON RACING\':fontcolor=white:fontsize=68:x=(w-text_w)/2:y=260,drawtext=fontfile='+font+':text=\'PC\':fontcolor=0x66e7ff:fontsize=42:x=(w-text_w)/2:y=355,fade=t=in:st=0:d=0.3,fade=t=out:st=2.4:d=0.6[end]')
filters.append(''.join(f'[v{i}]' for i in range(9))+'[end]concat=n=10:v=1:a=0[out]')
cmd += ['-filter_complex',';'.join(filters),'-map','[out]','-map','9:a','-c:v','libx264','-preset','medium','-crf','19','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart','-shortest','Polygon Racing PC - Trailer.mp4']
subprocess.run(cmd,check=True)


