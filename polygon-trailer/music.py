import numpy as np, wave
sr=44100; dur=30; n=sr*dur;t=np.arange(n)/sr; rng=np.random.default_rng(123)
y=np.zeros(n,dtype=np.float64);beat=60/132
notes=[57,60,64,67,64,60,55,60,53,57,60,65,60,57,52,55,52,55,59,64,59,55,50,55,53,57,60,65,67,64,60,57]
def add(start,signal):
 i=int(start*sr);k=min(len(signal),n-i)
 if k>0:y[i:i+k]+=signal[:k]
for k in range(int(dur/beat*2)):
 start=k*beat/2; midi=notes[k%len(notes)];f=440*2**((midi-69)/12); tt=np.arange(int(.32*sr))/sr; env=np.exp(-tt*7)*(1-np.exp(-tt*85)); add(start,(np.sin(2*np.pi*f*tt)+.24*np.sin(2*np.pi*2*f*tt)+.1*np.sin(2*np.pi*3*f*tt))*env*.09)
 if k%2==0:
  tt=np.arange(int(.28*sr))/sr; add(start,np.sin(2*np.pi*(100*tt-125*tt*tt))*np.exp(-tt*18)*.28)
 if k%4==2:
  tt=np.arange(int(.12*sr))/sr; add(start,rng.standard_normal(len(tt))*np.exp(-tt*28)*.07)
 if k%4==0:
  tt=np.arange(int(beat*2*sr))/sr; f=440*2**(((midi-24)-69)/12); env=np.minimum(1,tt*7)*np.maximum(0,np.minimum(1,(beat*2-tt)*5)); add(start,.08*np.sin(2*np.pi*f*tt)*env)
fade=np.minimum(1,t/1.2)*np.minimum(1,(dur-t)/1.5);y=np.tanh(y*1.3)*fade; samples=(y*25000).astype('<i2')
with wave.open('music.wav','wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(samples.tobytes())

