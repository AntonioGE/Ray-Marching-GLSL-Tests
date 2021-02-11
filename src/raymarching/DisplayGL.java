/*
 * MIT License
 * 
 * Copyright (c) 2021 Antonio GE
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
package raymarching;

import com.jogamp.common.nio.Buffers;
import com.jogamp.opengl.GL4;
import com.jogamp.opengl.GLAutoDrawable;
import com.jogamp.opengl.GLEventListener;
import com.jogamp.opengl.awt.GLJPanel;
import com.jogamp.opengl.util.FPSAnimator;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;
import java.nio.FloatBuffer;
import javax.swing.SwingUtilities;
import raymarching.math.vec.Vec3f;
import raymarching.utils.UtilsGL;

/**
 *
 * @author ANTONIO
 */
public class DisplayGL extends GLJPanel implements GLEventListener,
        MouseWheelListener, MouseListener, MouseMotionListener, KeyListener {

    private int shaderProgram;
    
    private int vao[] = new int[1];
    private int vbo[] = new int[1];
    private float[] positions = new float[]{
        -1.0f, 1.0f, 0.0f,
        1.0f, 1.0f, 0.0f,
        1.0f, -1.0f, 0.0f,
        -1.0f, -1.0f, 0.0f
    };
    
    private FPSAnimator animtr;
    
    private float lastMouseX, lastMouseY;
    
    private Cam cam = new Cam(new Vec3f(3.0f, 0.0f, 0.0f), new Vec3f(0.0f, 0.0f, 0.0f), 60.0f);
    private Vec3f lightPos = new Vec3f(3.0f, 0.0f, 3.0f);
    private float hueOffset = 0.0f;
    private float powerOffset = 0.0f;
    private float power = 0.0f;
    private float radius = 1.0f;
    
    public DisplayGL(){
        addGLEventListener(this);
        addMouseWheelListener(this);
        addMouseListener(this);
        addMouseMotionListener(this);
        addKeyListener(this);
    }
    
    @Override
    public void init(GLAutoDrawable glad) {
        GL4 gl = glad.getGL().getGL4();

        gl.glClearColor(0.5f, 0.0f, 1.0f, 1.0f);

        shaderProgram = UtilsGL.createShaderProgram(gl, "shaders/vert.vs", "shaders/mandelbulb.fs");

        gl.glGenVertexArrays(vao.length, vao, 0);
        gl.glBindVertexArray(vao[0]);
        gl.glGenBuffers(vbo.length, vbo, 0);

        gl.glBindBuffer(GL4.GL_ARRAY_BUFFER, vbo[0]);
        FloatBuffer vBuf = Buffers.newDirectFloatBuffer(positions);
        gl.glBufferData(GL4.GL_ARRAY_BUFFER, vBuf.limit() * Float.BYTES, vBuf, GL4.GL_STATIC_DRAW);

        animtr = new FPSAnimator(this, 60);
        animtr.start();

        UtilsGL.checkOpenGLError();
    }

    @Override
    public void dispose(GLAutoDrawable glad) {
    }

    @Override
    public void display(GLAutoDrawable glad) {
        GL4 gl = glad.getGL().getGL4();
        
        gl.glClear(GL4.GL_COLOR_BUFFER_BIT | GL4.GL_DEPTH_BUFFER_BIT);
        
        gl.glUseProgram(shaderProgram);
        
        int unifWinScale = gl.glGetUniformLocation(shaderProgram, "winScale");
        gl.glUniform2f(unifWinScale, 
                2.0f / getHeight(), 
                2.0f / getHeight());
        
        int unifWinOffset = gl.glGetUniformLocation(shaderProgram, "winOffset");
        gl.glUniform2f(unifWinOffset, 
                -((float)getWidth() / getHeight()), 
                -1.0f);
        
        int unifCamPos = gl.glGetUniformLocation(shaderProgram, "camPos");
        gl.glUniform3f(unifCamPos, cam.loc.x, cam.loc.y, cam.loc.z);
        
        int unifCamTransf = gl.glGetUniformLocation(shaderProgram, "camTransf");
        gl.glUniformMatrix3fv(unifCamTransf, 1, false, cam.getLocalAxis3f().toArray(), 0);
        
        int unifLightPos = gl.glGetUniformLocation(shaderProgram, "lightPos");
        gl.glUniform3f(unifLightPos, lightPos.x, lightPos.y, lightPos.z);
        
        int unifHueOffset = gl.glGetUniformLocation(shaderProgram, "hueOffset");
        gl.glUniform1f(unifHueOffset, hueOffset);
        
        int unifPower = gl.glGetUniformLocation(shaderProgram, "power");
        gl.glUniform1f(unifPower, power);
        
        int unifRadius = gl.glGetUniformLocation(shaderProgram, "radius");
        gl.glUniform1f(unifRadius, radius);
        
        lightPos.rotateAround(new Vec3f(0.0f, 0.0f, 0.0f), new Vec3f(0.0f, 0.0f, 1.0f), 0.01f);
        hueOffset = (hueOffset + 0.002f) % 1.0f;
        power = 9.0f;
        powerOffset =  powerOffset + 0.05f;
        //power = 4.0f * (float)Math.sin(powerOffset) + 8.0f;
        radius = 0.25f * (float)Math.sin(powerOffset) + 1.0f;
        
        gl.glBindBuffer(GL4.GL_ARRAY_BUFFER, vbo[0]);
        gl.glVertexAttribPointer(0, 3, GL4.GL_FLOAT, false, 0, 0);
        gl.glEnableVertexAttribArray(0);
        gl.glDrawArrays(GL4.GL_QUADS, 0, positions.length / 3);
        
        UtilsGL.checkOpenGLError();
        
        //cam.loc.print();
    }

    @Override
    public void reshape(GLAutoDrawable glad, int i, int i1, int i2, int i3) {
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent e) {
        float delta = 1.2f;
        if (e.getWheelRotation() > 0) {
            cam.moveTowardsTarget(cam.distToTarget * delta);
        } else {
            cam.moveTowardsTarget(cam.distToTarget / delta);
        }
        //repaint();
    }

    @Override
    public void mouseClicked(MouseEvent e) {
    }

    @Override
    public void mousePressed(MouseEvent e) {
        lastMouseX = e.getX();
        lastMouseY = e.getY();

    }

    @Override
    public void mouseReleased(MouseEvent e) {
    }

    @Override
    public void mouseEntered(MouseEvent e) {
    }

    @Override
    public void mouseExited(MouseEvent e) {
    }

    @Override
    public void mouseDragged(MouseEvent e) {
        if (SwingUtilities.isLeftMouseButton(e)) {
            final float deltaX = -(float) (e.getX() - lastMouseX) / (getWidth() / 2);
            final float deltaY = (float) (e.getY() - lastMouseY) / (getHeight() / 2);
            lastMouseX = e.getX();
            lastMouseY = e.getY();

            final float aspect = (float) getWidth() / getHeight();
            Vec3f trans = new Vec3f(
                    deltaX * cam.distToTarget * (float) Math.tan(Math.toRadians(cam.fov / 2.0f)) * aspect,
                    deltaY * cam.distToTarget * (float) Math.tan(Math.toRadians(cam.fov / 2.0f)),
                    0.0f
            );
            cam.loc.add(trans.mul(cam.getLocalAxis3f()));

        } else if (SwingUtilities.isRightMouseButton(e)) {
            float sensitivity = 2.0f;
            float deltaX = (e.getX() - lastMouseX) / sensitivity;
            float deltaY = (e.getY() - lastMouseY) / sensitivity;
            lastMouseX = e.getX();
            lastMouseY = e.getY();

            cam.orbit(new Vec3f(-deltaY, 0.0f, -deltaX));
        }
        //repaint();
    }

    @Override
    public void mouseMoved(MouseEvent e) {
    }

    @Override
    public void keyTyped(KeyEvent e) {
    }

    @Override
    public void keyPressed(KeyEvent e) {
    }

    @Override
    public void keyReleased(KeyEvent e) {
    }
    
    
}
