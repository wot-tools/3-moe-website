class CreateNations < ActiveRecord::Migration[5.0]
  def change
    create_table :nations do |t|
      t.string :name, null: false
	  t.integer :mark_count, null: false, default: 0

      t.timestamps
    end
	
	change_column :nations, :id, :string
  end
end
